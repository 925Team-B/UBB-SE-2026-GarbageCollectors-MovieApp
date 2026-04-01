#nullable enable
using System.Text.Json;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.Core.Services;

public sealed class MockAppService : ICatalogService, IReviewService, ICommentService, IBattleService, IPointService, IBadgeService
{
    private readonly object _sync = new();
    private readonly string _filePath;
    private MockDataFile _data;

    public MockAppService(string filePath)
    {
        _filePath = filePath;
        _data = LoadOrCreate();
    }

    public Task<List<Movie>> GetAllMovies()
    {
        lock (_sync)
        {
            var movies = _data.Movies
                .Select(m => new Movie
                {
                    MovieId = m.MovieId,
                    Title = m.Title,
                    Year = m.Year,
                    PosterUrl = m.PosterUrl,
                    Genre = m.Genre,
                    AverageRating = m.AverageRating
                })
                .OrderBy(m => m.Title)
                .ToList();

            return Task.FromResult(movies);
        }
    }

    public async Task<Movie> GetMovieById(int movieId)
    {
        var movie = (await GetAllMovies()).FirstOrDefault(m => m.MovieId == movieId);
        return movie ?? throw new InvalidOperationException($"Movie with ID {movieId} not found.");
    }

    public async Task<List<Movie>> SearchMovies(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllMovies();

        var movies = await GetAllMovies();
        return movies
            .Where(m => m.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<Movie>> FilterMovies(string genre, float minRating)
    {
        var movies = await GetAllMovies();

        if (!string.IsNullOrWhiteSpace(genre))
            movies = movies.Where(m => m.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase)).ToList();

        movies = movies.Where(m => m.AverageRating >= minRating).ToList();
        return movies;
    }

    public Task<List<Review>> GetReviewsForMovie(int movieId)
    {
        lock (_sync)
        {
            var reviews = BuildReviews()
                .Where(r => r.Movie?.MovieId == movieId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return Task.FromResult(reviews);
        }
    }

    public Task<Review> AddReview(int userId, int movieId, float rating, string content)
    {
        lock (_sync)
        {
            var user = _data.Users.FirstOrDefault(u => u.UserId == userId)
                ?? throw new InvalidOperationException("User not found.");
            var movie = _data.Movies.FirstOrDefault(m => m.MovieId == movieId)
                ?? throw new InvalidOperationException("Movie not found.");

            if (_data.Reviews.Any(r => r.UserId == userId && r.MovieId == movieId))
                throw new InvalidOperationException("User has already reviewed this movie.");

            if (rating < 0 || rating > 5 || (rating * 2) % 1 != 0)
                throw new InvalidOperationException("Rating must be between 0 and 5 in 0.5 increments.");

            if (!string.IsNullOrEmpty(content) && content.Length > 2000)
                throw new InvalidOperationException("Review content must not exceed 2000 characters.");

            if (!string.IsNullOrEmpty(content) && content.Length < 50)
                throw new InvalidOperationException("Review content must be at least 50 characters long.");

            var reviewId = _data.NextReviewId++;
            var entry = new ReviewEntry
            {
                ReviewId = reviewId,
                UserId = user.UserId,
                MovieId = movie.MovieId,
                StarRating = rating,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsExtraReview = false
            };

            _data.Reviews.Add(entry);
            RecalculateAverageRating_NoLock(movie.MovieId);
            AddPoints_NoLock(userId, movieId, IsMovieInActiveBattle_NoLock(movieId));
            Save_NoLock();

            return Task.FromResult(BuildReview(entry));
        }
    }

    public Task UpdateReview(int reviewId, float rating, string content)
    {
        lock (_sync)
        {
            var review = _data.Reviews.FirstOrDefault(r => r.ReviewId == reviewId)
                ?? throw new InvalidOperationException("Review not found.");

            if (rating < 0 || rating > 5 || (rating * 2) % 1 != 0)
                throw new InvalidOperationException("Rating must be between 0 and 5 in 0.5 increments.");

            if (!string.IsNullOrEmpty(content) && content.Length > 2000)
                throw new InvalidOperationException("Review content must not exceed 2000 characters.");

            if (!string.IsNullOrEmpty(content) && content.Length < 50)
                throw new InvalidOperationException("Review content must be at least 50 characters long.");

            review.StarRating = rating;
            review.Content = content;
            RecalculateAverageRating_NoLock(review.MovieId);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task DeleteReview(int reviewId)
    {
        lock (_sync)
        {
            var review = _data.Reviews.FirstOrDefault(r => r.ReviewId == reviewId)
                ?? throw new InvalidOperationException("Review not found.");

            _data.Reviews.Remove(review);
            RecalculateAverageRating_NoLock(review.MovieId);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task SubmitExtraReview(int reviewId, int cgRating, string cgText, int actingRating, string actingText, int plotRating, string plotText, int soundRating, string soundText, int cinRating, string cinText, string mainExtraText)
    {
        lock (_sync)
        {
            var review = _data.Reviews.FirstOrDefault(r => r.ReviewId == reviewId)
                ?? throw new InvalidOperationException("Review not found.");

            if (string.IsNullOrEmpty(mainExtraText) || mainExtraText.Length < 500 || mainExtraText.Length > 12000)
                throw new InvalidOperationException("Main extra text must be between 500 and 12000 characters.");

            ValidateCategoryText(cgText, "CGI");
            ValidateCategoryText(actingText, "Acting");
            ValidateCategoryText(plotText, "Plot");
            ValidateCategoryText(soundText, "Sound");
            ValidateCategoryText(cinText, "Cinematography");

            ValidateCategoryRating(cgRating, "CGI");
            ValidateCategoryRating(actingRating, "Acting");
            ValidateCategoryRating(plotRating, "Plot");
            ValidateCategoryRating(soundRating, "Sound");
            ValidateCategoryRating(cinRating, "Cinematography");

            review.CgiRating = cgRating;
            review.CgiText = cgText;
            review.ActingRating = actingRating;
            review.ActingText = actingText;
            review.PlotRating = plotRating;
            review.PlotText = plotText;
            review.SoundRating = soundRating;
            review.SoundText = soundText;
            review.CinematographyRating = cinRating;
            review.CinematographyText = cinText;
            review.Content = mainExtraText;
            review.IsExtraReview = true;

            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task<double> GetAverageRating(int movieId)
    {
        lock (_sync)
        {
            var reviews = _data.Reviews.Where(r => r.MovieId == movieId).ToList();
            if (reviews.Count == 0)
                return Task.FromResult(0d);

            return Task.FromResult(Math.Round(reviews.Average(r => r.StarRating), 1));
        }
    }

    public Task<List<Comment>> GetCommentsForMovie(int movieId)
    {
        lock (_sync)
        {
            var comments = BuildComments()
                .Where(c => c.Movie?.MovieId == movieId)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return Task.FromResult(comments);
        }
    }

    public Task<Comment> AddComment(int userId, int movieId, string content)
    {
        lock (_sync)
        {
            if (!string.IsNullOrEmpty(content) && content.Length > 10000)
                throw new InvalidOperationException("Comment content must not exceed 10000 characters.");

            if (_data.Users.All(u => u.UserId != userId))
                throw new InvalidOperationException("User not found.");
            if (_data.Movies.All(m => m.MovieId != movieId))
                throw new InvalidOperationException("Movie not found.");

            var commentId = _data.NextCommentId++;
            var entry = new CommentEntry
            {
                MessageId = commentId,
                AuthorId = userId,
                MovieId = movieId,
                ParentCommentId = null,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _data.Comments.Add(entry);
            Save_NoLock();
            return Task.FromResult(BuildComment(entry));
        }
    }

    public Task<Comment> AddReply(int userId, int parentCommentId, string content)
    {
        lock (_sync)
        {
            var parent = _data.Comments.FirstOrDefault(c => c.MessageId == parentCommentId)
                ?? throw new InvalidOperationException("Parent comment not found.");

            if (!string.IsNullOrEmpty(content) && content.Length > 10000)
                throw new InvalidOperationException("Comment content must not exceed 10000 characters.");

            if (_data.Users.All(u => u.UserId != userId))
                throw new InvalidOperationException("User not found.");

            var commentId = _data.NextCommentId++;
            var entry = new CommentEntry
            {
                MessageId = commentId,
                AuthorId = userId,
                MovieId = parent.MovieId,
                ParentCommentId = parentCommentId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _data.Comments.Add(entry);
            Save_NoLock();
            return Task.FromResult(BuildComment(entry));
        }
    }

    public Task DeleteComment(int commentId)
    {
        lock (_sync)
        {
            var comment = _data.Comments.FirstOrDefault(c => c.MessageId == commentId)
                ?? throw new InvalidOperationException("Comment not found.");

            _data.Comments.Remove(comment);
            _data.Comments.RemoveAll(c => c.ParentCommentId == commentId);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task<Battle?> GetActiveBattle()
    {
        lock (_sync)
        {
            var battle = BuildBattles().FirstOrDefault(b => b.Status == "Active");
            return Task.FromResult(battle);
        }
    }

    public Task<Battle> CreateBattle(int firstMovieId, int secondMovieId)
    {
        lock (_sync)
        {
            if (_data.Battles.Any(b => b.Status == "Active"))
                throw new InvalidOperationException("An active battle already exists.");

            var first = _data.Movies.FirstOrDefault(m => m.MovieId == firstMovieId)
                ?? throw new InvalidOperationException("First movie not found.");
            var second = _data.Movies.FirstOrDefault(m => m.MovieId == secondMovieId)
                ?? throw new InvalidOperationException("Second movie not found.");

            if (Math.Abs(first.AverageRating - second.AverageRating) > 0.5)
                throw new InvalidOperationException("Rating difference between movies must be 0.5 or less.");

            var today = DateTime.UtcNow.Date;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            var startDate = today.AddDays(daysUntilMonday);
            var endDate = startDate.AddDays(6);

            var id = _data.NextBattleId++;
            var entry = new BattleEntry
            {
                BattleId = id,
                FirstMovieId = firstMovieId,
                SecondMovieId = secondMovieId,
                InitialRatingFirstMovie = first.AverageRating,
                InitialRatingSecondMovie = second.AverageRating,
                StartDate = startDate,
                EndDate = endDate,
                Status = "Active"
            };

            _data.Battles.Add(entry);
            Save_NoLock();
            return Task.FromResult(BuildBattle(entry));
        }
    }

    public Task<Bet> PlaceBet(int userId, int battleId, int movieId, int amount)
    {
        lock (_sync)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Bet amount must be greater than 0.");

            if (_data.Bets.Any(b => b.UserId == userId && b.BattleId == battleId))
                throw new InvalidOperationException("User has already placed a bet on this battle.");

            if (_data.Users.All(u => u.UserId != userId))
                throw new InvalidOperationException("User not found.");
            if (_data.Battles.All(b => b.BattleId != battleId))
                throw new InvalidOperationException("Battle not found.");
            if (_data.Movies.All(m => m.MovieId != movieId))
                throw new InvalidOperationException("Movie not found.");

            FreezePoints_NoLock(userId, amount);

            var entry = new BetEntry
            {
                UserId = userId,
                BattleId = battleId,
                MovieId = movieId,
                Amount = amount
            };
            _data.Bets.Add(entry);
            Save_NoLock();
            return Task.FromResult(BuildBet(entry));
        }
    }

    public Task<Bet?> GetBet(int userId, int battleId)
    {
        lock (_sync)
        {
            var bet = _data.Bets.FirstOrDefault(b => b.UserId == userId && b.BattleId == battleId);
            return Task.FromResult(bet is null ? null : BuildBet(bet));
        }
    }

    public Task<int> DetermineWinner(int battleId)
    {
        lock (_sync)
        {
            var battle = _data.Battles.FirstOrDefault(b => b.BattleId == battleId)
                ?? throw new InvalidOperationException("Battle not found.");

            var firstMovie = _data.Movies.FirstOrDefault(m => m.MovieId == battle.FirstMovieId);
            var secondMovie = _data.Movies.FirstOrDefault(m => m.MovieId == battle.SecondMovieId);

            var firstImprovement = (firstMovie?.AverageRating ?? 0) - battle.InitialRatingFirstMovie;
            var secondImprovement = (secondMovie?.AverageRating ?? 0) - battle.InitialRatingSecondMovie;

            return Task.FromResult(firstImprovement >= secondImprovement ? battle.FirstMovieId : battle.SecondMovieId);
        }
    }

    public async Task DistributePayouts(int battleId)
    {
        var winningMovieId = await DetermineWinner(battleId);

        lock (_sync)
        {
            var battle = _data.Battles.FirstOrDefault(b => b.BattleId == battleId)
                ?? throw new InvalidOperationException("Battle not found.");

            var bets = _data.Bets.Where(b => b.BattleId == battleId).ToList();
            foreach (var bet in bets)
            {
                if (bet.MovieId == winningMovieId)
                    RefundPoints_NoLock(bet.UserId, bet.Amount * 2);
            }

            battle.Status = "Finished";
            Save_NoLock();
        }
    }

    public Task<Battle?> GetCurrentBattleForUser(int userId)
    {
        lock (_sync)
        {
            // Return active battle if one exists
            var active = BuildBattles().FirstOrDefault(b => b.Status == "Active");
            if (active != null)
                return Task.FromResult<Battle?>(active);

            // No active battle — show the most recent battle regardless
            var recentBattle = _data.Battles
                .OrderByDescending(b => b.EndDate)
                .Select(BuildBattle)
                .FirstOrDefault();

            return Task.FromResult<Battle?>(recentBattle);
        }
    }

    public async Task SettleExpiredBattlesAsync()
    {
        var today = DateTime.UtcNow.Date;
        List<int> expiredIds;
        lock (_sync)
        {
            expiredIds = _data.Battles
                .Where(b => b.Status == "Active" && b.EndDate < today)
                .Select(b => b.BattleId)
                .ToList();
        }
        foreach (var id in expiredIds)
            await DistributePayouts(id);
    }

    public async Task ForceSettleBattleAsync(int battleId) => await DistributePayouts(battleId);

    public async Task ResetAllBattlesForDemoAsync()
    {
        // Refund frozen points for active bets
        List<(int UserId, int Amount)> refunds;
        lock (_sync)
        {
            refunds = _data.Bets
                .Join(_data.Battles.Where(b => b.Status == "Active"),
                      bet => bet.BattleId, battle => battle.BattleId,
                      (bet, _) => (bet.UserId, bet.Amount))
                .ToList();
        }
        foreach (var (userId, amount) in refunds)
            await RefundPoints(userId, amount);

        lock (_sync)
        {
            _data.Bets.Clear();
            _data.Battles.Clear();
            Save_NoLock();
        }
    }

    public async Task<Battle> CreateDemoBattleAsync()
    {
        lock (_sync)
        {
            var movies = _data.Movies
                .Where(m => m.AverageRating > 0)
                .OrderBy(m => m.AverageRating)
                .ToList();

            var validPairs = new List<(Movie First, Movie Second)>();
            for (int i = 0; i < movies.Count - 1; i++)
                for (int j = i + 1; j < movies.Count; j++)
                    if (Math.Abs(movies[i].AverageRating - movies[j].AverageRating) <= 0.5)
                        validPairs.Add((movies[i], movies[j]));

            if (validPairs.Count == 0)
                throw new InvalidOperationException("No suitable movie pair found.");

            var chosen = validPairs[new Random().Next(validPairs.Count)];
            var (first, second) = chosen;

            var today = DateTime.UtcNow.Date;
            var id = (_data.Battles.Any() ? _data.Battles.Max(b => b.BattleId) : 0) + 1;
            var entry = new BattleEntry
            {
                BattleId = id,
                FirstMovieId = first.MovieId,
                SecondMovieId = second.MovieId,
                InitialRatingFirstMovie = first.AverageRating,
                InitialRatingSecondMovie = second.AverageRating,
                StartDate = today,
                EndDate = today.AddDays(6),
                Status = "Active"
            };
            _data.Battles.Add(entry);
            Save_NoLock();
            return BuildBattle(entry);
        }
    }

    public Task<UserStats> GetUserStats(int userId)
    {
        lock (_sync)
        {
            var user = _data.Users.FirstOrDefault(u => u.UserId == userId)
                ?? throw new InvalidOperationException("User not found.");

            var stats = _data.UserStats.FirstOrDefault(s => s.UserId == userId);
            if (stats is null)
            {
                stats = new UserStatsEntry
                {
                    StatsId = _data.NextStatsId++,
                    UserId = userId,
                    TotalPoints = 0,
                    WeeklyScore = 0
                };
                _data.UserStats.Add(stats);
                Save_NoLock();
            }

            return Task.FromResult(new UserStats
            {
                StatsId = stats.StatsId,
                TotalPoints = stats.TotalPoints,
                WeeklyScore = stats.WeeklyScore,
                User = new User { UserId = user.UserId }
            });
        }
    }

    public Task AddPoints(int userId, int movieId, bool isBattleMovie)
    {
        lock (_sync)
        {
            AddPoints_NoLock(userId, movieId, isBattleMovie);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task DeductPoints(int userId, int points)
    {
        lock (_sync)
        {
            var stats = EnsureStats_NoLock(userId);
            stats.TotalPoints = Math.Max(0, stats.TotalPoints - points);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task FreezePoints(int userId, int amount)
    {
        lock (_sync)
        {
            FreezePoints_NoLock(userId, amount);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task RefundPoints(int userId, int amount)
    {
        lock (_sync)
        {
            RefundPoints_NoLock(userId, amount);
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task UpdateWeeklyScore(int userId)
    {
        lock (_sync)
        {
            var stats = EnsureStats_NoLock(userId);
            stats.WeeklyScore = stats.TotalPoints;
            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    public Task<List<Badge>> GetUserBadges(int userId)
    {
        lock (_sync)
        {
            var badgeIds = _data.UserBadges.Where(ub => ub.UserId == userId).Select(ub => ub.BadgeId).ToHashSet();
            return Task.FromResult(_data.Badges.Where(b => badgeIds.Contains(b.BadgeId)).Select(CloneBadge).ToList());
        }
    }

    public Task<List<Badge>> GetAllBadges()
    {
        lock (_sync)
        {
            return Task.FromResult(_data.Badges.Select(CloneBadge).ToList());
        }
    }

    public Task CheckAndAwardBadges(int userId)
    {
        lock (_sync)
        {
            var existingBadgeIds = _data.UserBadges.Where(ub => ub.UserId == userId).Select(ub => ub.BadgeId).ToHashSet();
            var userReviews = _data.Reviews.Where(r => r.UserId == userId).ToList();
            var totalReviews = userReviews.Count;
            var extraReviews = userReviews.Count(r => r.IsExtraReview);
            var fullyCompletedExtraReviews = userReviews.Count(r =>
                r.IsExtraReview &&
                !string.IsNullOrEmpty(r.CinematographyText) &&
                !string.IsNullOrEmpty(r.ActingText) &&
                !string.IsNullOrEmpty(r.CgiText) &&
                !string.IsNullOrEmpty(r.PlotText) &&
                !string.IsNullOrEmpty(r.SoundText));

            var comedyMovieIds = _data.Movies
                .Where(m => m.Genre.Equals("Comedy", StringComparison.OrdinalIgnoreCase))
                .Select(m => m.MovieId)
                .ToHashSet();
            var comedyReviews = userReviews.Count(r => comedyMovieIds.Contains(r.MovieId));
            var comedyPercentage = totalReviews > 0 ? (double)comedyReviews / totalReviews * 100 : 0;

            foreach (var badge in _data.Badges)
            {
                if (existingBadgeIds.Contains(badge.BadgeId))
                    continue;

                var shouldAward = badge.Name switch
                {
                    "The Snob" => extraReviews >= 10,
                    "The Super Serious" => fullyCompletedExtraReviews >= 50,
                    "The Joker" => comedyPercentage > 70,
                    "The Godfather I" => totalReviews >= 100,
                    "The Godfather II" => totalReviews >= 200,
                    "The Godfather III" => totalReviews >= 300,
                    _ => false
                };

                if (shouldAward)
                    _data.UserBadges.Add(new UserBadgeEntry { UserId = userId, BadgeId = badge.BadgeId });
            }

            Save_NoLock();
            return Task.CompletedTask;
        }
    }

    private List<Review> BuildReviews() => _data.Reviews.Select(BuildReview).ToList();

    private Review BuildReview(ReviewEntry review)
    {
        var user = _data.Users.First(u => u.UserId == review.UserId);
        var movie = _data.Movies.First(m => m.MovieId == review.MovieId);

        return new Review
        {
            ReviewId = review.ReviewId,
            StarRating = review.StarRating,
            Content = review.Content,
            CreatedAt = review.CreatedAt,
            IsExtraReview = review.IsExtraReview,
            CinematographyRating = review.CinematographyRating,
            CinematographyText = review.CinematographyText,
            ActingRating = review.ActingRating,
            ActingText = review.ActingText,
            CgiRating = review.CgiRating,
            CgiText = review.CgiText,
            PlotRating = review.PlotRating,
            PlotText = review.PlotText,
            SoundRating = review.SoundRating,
            SoundText = review.SoundText,
            User = new User { UserId = user.UserId },
            Movie = new Movie
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Year = movie.Year,
                PosterUrl = movie.PosterUrl,
                Genre = movie.Genre,
                AverageRating = movie.AverageRating
            }
        };
    }

    private List<Comment> BuildComments()
    {
        var comments = _data.Comments.Select(BuildComment).ToDictionary(c => c.MessageId);
        foreach (var entry in _data.Comments)
        {
            if (entry.ParentCommentId.HasValue && comments.TryGetValue(entry.ParentCommentId.Value, out var parent))
            {
                comments[entry.MessageId].ParentComment = new Comment { MessageId = parent.MessageId };
            }
        }

        return comments.Values.ToList();
    }

    private Comment BuildComment(CommentEntry entry)
    {
        var user = _data.Users.First(u => u.UserId == entry.AuthorId);
        var movie = _data.Movies.First(m => m.MovieId == entry.MovieId);

        return new Comment
        {
            MessageId = entry.MessageId,
            Content = entry.Content,
            CreatedAt = entry.CreatedAt,
            Author = new User { UserId = user.UserId },
            Movie = new Movie
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Year = movie.Year,
                PosterUrl = movie.PosterUrl,
                Genre = movie.Genre,
                AverageRating = movie.AverageRating
            },
            ParentComment = entry.ParentCommentId.HasValue ? new Comment { MessageId = entry.ParentCommentId.Value } : null,
            Replies = new List<Comment>()
        };
    }

    private List<Battle> BuildBattles() => _data.Battles.Select(BuildBattle).ToList();

    private Battle BuildBattle(BattleEntry battle)
    {
        var first = _data.Movies.First(m => m.MovieId == battle.FirstMovieId);
        var second = _data.Movies.First(m => m.MovieId == battle.SecondMovieId);

        return new Battle
        {
            BattleId = battle.BattleId,
            InitialRatingFirstMovie = battle.InitialRatingFirstMovie,
            InitialRatingSecondMovie = battle.InitialRatingSecondMovie,
            StartDate = battle.StartDate,
            EndDate = battle.EndDate,
            Status = battle.Status,
            FirstMovie = new Movie
            {
                MovieId = first.MovieId,
                Title = first.Title,
                Year = first.Year,
                PosterUrl = first.PosterUrl,
                Genre = first.Genre,
                AverageRating = first.AverageRating
            },
            SecondMovie = new Movie
            {
                MovieId = second.MovieId,
                Title = second.Title,
                Year = second.Year,
                PosterUrl = second.PosterUrl,
                Genre = second.Genre,
                AverageRating = second.AverageRating
            }
        };
    }

    private Bet BuildBet(BetEntry bet)
    {
        var user = _data.Users.First(u => u.UserId == bet.UserId);
        var battle = _data.Battles.First(b => b.BattleId == bet.BattleId);
        var movie = _data.Movies.First(m => m.MovieId == bet.MovieId);

        return new Bet
        {
            Amount = bet.Amount,
            User = new User { UserId = user.UserId },
            Battle = BuildBattle(battle),
            Movie = new Movie
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Year = movie.Year,
                PosterUrl = movie.PosterUrl,
                Genre = movie.Genre,
                AverageRating = movie.AverageRating
            }
        };
    }

    private void RecalculateAverageRating_NoLock(int movieId)
    {
        var movie = _data.Movies.FirstOrDefault(m => m.MovieId == movieId);
        if (movie is null) return;

        var reviews = _data.Reviews.Where(r => r.MovieId == movieId).ToList();
        movie.AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.StarRating), 1);
    }

    private bool IsMovieInActiveBattle_NoLock(int movieId)
    {
        return _data.Battles.Any(b => b.Status == "Active" && (b.FirstMovieId == movieId || b.SecondMovieId == movieId));
    }

    private void AddPoints_NoLock(int userId, int movieId, bool isBattleMovie)
    {
        var stats = EnsureStats_NoLock(userId);
        var movie = _data.Movies.FirstOrDefault(m => m.MovieId == movieId);
        if (movie is null) return;

        var pointsToAdd = 0;
        if (movie.AverageRating > 3.5)
            pointsToAdd += 2;
        else if (movie.AverageRating < 2.0)
            pointsToAdd += 1;

        if (isBattleMovie)
            pointsToAdd += 5;

        stats.TotalPoints = Math.Max(0, stats.TotalPoints + pointsToAdd);
        CheckAndAwardBadges(userId).GetAwaiter().GetResult();
    }

    private void FreezePoints_NoLock(int userId, int amount)
    {
        var stats = EnsureStats_NoLock(userId);
        if (stats.TotalPoints < amount)
            throw new InvalidOperationException($"Insufficient points. You have {stats.TotalPoints} but need {amount}.");

        stats.TotalPoints -= amount;
    }

    private void RefundPoints_NoLock(int userId, int amount)
    {
        var stats = EnsureStats_NoLock(userId);
        stats.TotalPoints += amount;
    }

    private UserStatsEntry EnsureStats_NoLock(int userId)
    {
        var user = _data.Users.FirstOrDefault(u => u.UserId == userId)
            ?? throw new InvalidOperationException("User not found.");

        var stats = _data.UserStats.FirstOrDefault(s => s.UserId == user.UserId);
        if (stats is null)
        {
            stats = new UserStatsEntry
            {
                StatsId = _data.NextStatsId++,
                UserId = user.UserId,
                TotalPoints = 0,
                WeeklyScore = 0
            };
            _data.UserStats.Add(stats);
        }

        return stats;
    }

    private static void ValidateCategoryText(string text, string categoryName)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 50 || text.Length > 2000)
            throw new InvalidOperationException($"{categoryName} text must be between 50 and 2000 characters.");
    }

    private static void ValidateCategoryRating(int rating, string categoryName)
    {
        if (rating < 0 || rating > 5)
            throw new InvalidOperationException($"{categoryName} rating must be between 0 and 5.");
    }

    private static Badge CloneBadge(Badge badge) => new()
    {
        BadgeId = badge.BadgeId,
        Name = badge.Name,
        CriteriaValue = badge.CriteriaValue
    };

    private MockDataFile LoadOrCreate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (!File.Exists(_filePath))
        {
            var initial = CreateInitialData();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(initial, JsonOptions));
            return initial;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<MockDataFile>(json, JsonOptions);
            return loaded ?? CreateInitialData();
        }
        catch
        {
            var initial = CreateInitialData();
            File.WriteAllText(_filePath, JsonSerializer.Serialize(initial, JsonOptions));
            return initial;
        }
    }

    private void Save_NoLock()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, JsonOptions));
    }

    private static MockDataFile CreateInitialData()
    {
        return new MockDataFile
        {
            NextReviewId = 3,
            NextCommentId = 4,
            NextBattleId = 2,
            NextStatsId = 4,
            Users =
            [
                new User { UserId = 1 },
                new User { UserId = 2 },
                new User { UserId = 3 }
            ],
            Movies =
            [
                new Movie { MovieId = 1, Title = "The Shawshank Redemption", Year = 1994, Genre = "Drama", PosterUrl = "https://m.media-amazon.com/images/M/MV5BMDAyY2FhYjctNDc5OS00MDNlLThiMGUtY2UxYWVkNGY2ZjljXkEyXkFqcGc@._V1_.jpg", AverageRating = 4.5 },
                new Movie { MovieId = 2, Title = "The Dark Knight", Year = 2008, Genre = "Action", PosterUrl = "https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg", AverageRating = 4.3 },
                new Movie { MovieId = 3, Title = "Inception", Year = 2010, Genre = "Sci-Fi", PosterUrl = "https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg", AverageRating = 4.2 },
                new Movie { MovieId = 4, Title = "Pulp Fiction", Year = 1994, Genre = "Crime", PosterUrl = "https://m.media-amazon.com/images/M/MV5BNGNhMDIzZTUtNTBlZi00MTRlLWFjMDYtZjYwMjY2ZWU5ZjljXkEyXkFqcGdeQXVyNzkwMjQ5NzM@._V1_.jpg", AverageRating = 4.4 },
                new Movie { MovieId = 5, Title = "The Hangover", Year = 2009, Genre = "Comedy", PosterUrl = "https://m.media-amazon.com/images/M/MV5BNGQwZjg5YmYtY2VkNC00NzliLTljYTctNzI5NmU3MjE2ODQzXkEyXkFqcGdeQXVyNzkwMjQ5NzM@._V1_.jpg", AverageRating = 3.5 },
                new Movie { MovieId = 6, Title = "Interstellar", Year = 2014, Genre = "Sci-Fi", PosterUrl = "https://m.media-amazon.com/images/M/MV5BZjdkOTU3MDktN2IxOS00OGEyLWFmMjktY2FiMmZkNWIyODZiXkEyXkFqcGdeQXVyMTMxODk2OTU@._V1_.jpg", AverageRating = 4.6 }
            ],
            Reviews =
            [
                new ReviewEntry { ReviewId = 1, UserId = 2, MovieId = 1, StarRating = 4.5f, Content = "Excellent movie with brilliant performances and unforgettable ending that still holds up after many years.", CreatedAt = DateTime.UtcNow.AddDays(-4), IsExtraReview = false },
                new ReviewEntry { ReviewId = 2, UserId = 3, MovieId = 2, StarRating = 4.0f, Content = "Strong superhero film with great pacing and amazing villain performance that elevates the entire experience.", CreatedAt = DateTime.UtcNow.AddDays(-3), IsExtraReview = false }
            ],
            Comments =
            [
                new CommentEntry { MessageId = 1, AuthorId = 2, MovieId = 1, ParentCommentId = null, Content = "Still one of my favorites.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new CommentEntry { MessageId = 2, AuthorId = 1, MovieId = 1, ParentCommentId = 1, Content = "Same here, the ending is perfect.", CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(2) },
                new CommentEntry { MessageId = 3, AuthorId = 3, MovieId = 2, ParentCommentId = null, Content = "Heath Ledger was incredible.", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            ],
            Battles =
            [
                new BattleEntry { BattleId = 1, FirstMovieId = 1, SecondMovieId = 4, InitialRatingFirstMovie = 4.5, InitialRatingSecondMovie = 4.4, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(6), Status = "Active" }
            ],
            Bets = [],
            UserStats =
            [
                new UserStatsEntry { StatsId = 1, UserId = 1, TotalPoints = 50, WeeklyScore = 10 },
                new UserStatsEntry { StatsId = 2, UserId = 2, TotalPoints = 30, WeeklyScore = 5 },
                new UserStatsEntry { StatsId = 3, UserId = 3, TotalPoints = 20, WeeklyScore = 3 }
            ],
            Badges =
            [
                new Badge { BadgeId = 1, Name = "The Snob", CriteriaValue = 10 },
                new Badge { BadgeId = 2, Name = "The Super Serious", CriteriaValue = 50 },
                new Badge { BadgeId = 3, Name = "The Joker", CriteriaValue = 70 },
                new Badge { BadgeId = 4, Name = "The Godfather I", CriteriaValue = 100 },
                new Badge { BadgeId = 5, Name = "The Godfather II", CriteriaValue = 200 },
                new Badge { BadgeId = 6, Name = "The Godfather III", CriteriaValue = 300 }
            ],
            UserBadges = []
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private sealed class MockDataFile
    {
        public int NextReviewId { get; set; }
        public int NextCommentId { get; set; }
        public int NextBattleId { get; set; }
        public int NextStatsId { get; set; }
        public List<User> Users { get; set; } = [];
        public List<Movie> Movies { get; set; } = [];
        public List<ReviewEntry> Reviews { get; set; } = [];
        public List<CommentEntry> Comments { get; set; } = [];
        public List<BattleEntry> Battles { get; set; } = [];
        public List<BetEntry> Bets { get; set; } = [];
        public List<UserStatsEntry> UserStats { get; set; } = [];
        public List<Badge> Badges { get; set; } = [];
        public List<UserBadgeEntry> UserBadges { get; set; } = [];
    }

    private sealed class ReviewEntry
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public int MovieId { get; set; }
        public float StarRating { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsExtraReview { get; set; }
        public int CinematographyRating { get; set; }
        public string? CinematographyText { get; set; }
        public int ActingRating { get; set; }
        public string? ActingText { get; set; }
        public int CgiRating { get; set; }
        public string? CgiText { get; set; }
        public int PlotRating { get; set; }
        public string? PlotText { get; set; }
        public int SoundRating { get; set; }
        public string? SoundText { get; set; }
    }

    private sealed class CommentEntry
    {
        public int MessageId { get; set; }
        public int AuthorId { get; set; }
        public int MovieId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private sealed class BattleEntry
    {
        public int BattleId { get; set; }
        public int FirstMovieId { get; set; }
        public int SecondMovieId { get; set; }
        public double InitialRatingFirstMovie { get; set; }
        public double InitialRatingSecondMovie { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    private sealed class BetEntry
    {
        public int UserId { get; set; }
        public int BattleId { get; set; }
        public int MovieId { get; set; }
        public int Amount { get; set; }
    }

    private sealed class UserStatsEntry
    {
        public int StatsId { get; set; }
        public int UserId { get; set; }
        public int TotalPoints { get; set; }
        public int WeeklyScore { get; set; }
    }

    private sealed class UserBadgeEntry
    {
        public int UserId { get; set; }
        public int BadgeId { get; set; }
    }
}
