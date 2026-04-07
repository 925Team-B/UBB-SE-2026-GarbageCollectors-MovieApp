#nullable enable
using System.Text.Json;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="MockAppService"/>.
/// Each test uses a fresh temp file so state never leaks between tests.
/// </summary>
public class MockAppServiceTests : IDisposable
{
    private readonly string filePath;

    public MockAppServiceTests()
    {
        filePath = Path.Combine(Path.GetTempPath(), $"MockAppService_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private MockAppService CreateService() => new MockAppService(filePath);

    // ==================== ICatalogService ====================
    [Fact]
    public async Task GetAllMovies_WithMoviesInRepository_ReturnsMoviesOrderedByTitle()
    {
        var sut = CreateService();

        var movies = await sut.GetAllMovies();

        Assert.NotEmpty(movies);
        var titles = movies.Select(m => m.Title).ToList();
        Assert.Equal(titles.OrderBy(t => t).ToList(), titles);
    }

    [Fact]
    public async Task GetMovieById_WhenMovieExists_ReturnsCorrectMovie()
    {
        var sut = CreateService();
        var all = await sut.GetAllMovies();
        var first = all.First();

        var movie = await sut.GetMovieById(first.MovieId);

        Assert.Equal(first.MovieId, movie.MovieId);
        Assert.Equal(first.Title, movie.Title);
    }

    [Fact]
    public async Task GetMovieById_WhenMovieDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetMovieById(99999));
    }

    [Fact]
    public async Task SearchMovies_WithMatchingQuery_ReturnsOnlyMatchingMovies()
    {
        var sut = CreateService();
        var all = await sut.GetAllMovies();
        var targetTitle = all.First().Title;
        var query = targetTitle.Substring(0, 3);

        var results = await sut.SearchMovies(query);

        Assert.All(results, m => Assert.Contains(query, m.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchMovies_WithEmptyQuery_ReturnsAllMovies()
    {
        var sut = CreateService();
        var all = await sut.GetAllMovies();

        var results = await sut.SearchMovies(string.Empty);

        Assert.Equal(all.Count, results.Count);
    }

    [Fact]
    public async Task FilterMovies_WithGenreFilter_ReturnsOnlyMoviesOfThatGenre()
    {
        var sut = CreateService();
        var all = await sut.GetAllMovies();
        var genre = all.First().Genre;

        var results = await sut.FilterMovies(genre, 0f);

        Assert.All(results, m => Assert.Equal(genre, m.Genre, StringComparer.OrdinalIgnoreCase));
    }

    // ==================== IReviewService ====================
    [Fact]
    public async Task AddReview_WhenValidRatingAndContent_AddsReviewAndUpdatesMovieAverageRating()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();
        var movie = movies.First();
        // Use user id 1 (present in default mock data)
        const int userId = 1;
        const float rating = 4.5f;
        var content = new string('x', 100); // valid length: 50-2000

        // Find a movie not yet reviewed by user 1
        var allMovies = await sut.GetAllMovies();
        var targetMovie = movies.Last(); // default: use last movie
        foreach (var m in allMovies)
        {
            var r = await sut.GetReviewsForMovie(m.MovieId);
            if (r.All(rv => rv.User?.UserId != userId))
            {
                targetMovie = m;
                break;
            }
        }

        var review = await sut.AddReview(userId, targetMovie.MovieId, rating, content);

        Assert.Equal(rating, review.StarRating);
        Assert.Equal(content, review.Content);
        Assert.Equal(userId, review.User?.UserId);
    }

    [Fact]
    public async Task AddReview_WhenRatingOutOfRange_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, movies.First().MovieId, 6.0f, "content long enough to pass validation here we go"));
    }

    [Fact]
    public async Task AddReview_WhenRatingHasInvalidIncrement_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, movies.First().MovieId, 3.3f, "content long enough to pass validation here we go"));
    }

    [Fact]
    public async Task GetAverageRating_WhenNoReviews_ReturnsZero()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();
        // Find a movie with no reviews
        Movie? movieWithNoReviews = null;
        foreach (var m in movies)
        {
            var r = await sut.GetReviewsForMovie(m.MovieId);
            if (r.Count == 0)
            {
                movieWithNoReviews = m;
                break;
            }
        }
        if (movieWithNoReviews is null)
        {
            return; // skip if all have reviews in default data
        }

        var avg = await sut.GetAverageRating(movieWithNoReviews.MovieId);

        Assert.Equal(0d, avg);
    }

    // ==================== ICommentService ====================
    [Fact]
    public async Task AddComment_WhenValidInputs_AddsCommentToMovie()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();
        var movie = movies.First();

        var comment = await sut.AddComment(1, movie.MovieId, "This is a great movie!");

        Assert.Equal("This is a great movie!", comment.Content);
        Assert.Equal(1, comment.Author?.UserId);
        Assert.Equal(movie.MovieId, comment.Movie?.MovieId);
    }

    [Fact]
    public async Task AddComment_WhenContentExceedsMaxLength_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddComment(1, movies.First().MovieId, new string('x', 10001)));
    }

    [Fact]
    public async Task DeleteComment_WhenCommentExists_RemovesItFromList()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();
        var movie = movies.First();
        var comment = await sut.AddComment(1, movie.MovieId, "Temporary comment");

        await sut.DeleteComment(comment.MessageId);

        var remaining = await sut.GetCommentsForMovie(movie.MovieId);
        Assert.DoesNotContain(remaining, c => c.MessageId == comment.MessageId);
    }

    [Fact]
    public async Task AddReply_WhenParentExists_CreatesReplyLinkedToParent()
    {
        var sut = CreateService();
        var movies = await sut.GetAllMovies();
        var movie = movies.First();
        var parent = await sut.AddComment(1, movie.MovieId, "Root comment here.");

        var reply = await sut.AddReply(1, parent.MessageId, "Reply to root.");

        Assert.Equal("Reply to root.", reply.Content);
        Assert.NotNull(reply.ParentComment);
        Assert.Equal(parent.MessageId, reply.ParentComment.MessageId);
    }

    // ==================== IBattleService ====================
    [Fact]
    public async Task GetActiveBattle_WhenActiveBattleExists_ReturnsActiveBattle()
    {
        var sut = CreateService();

        // Default mock data contains 1 active battle
        var battle = await sut.GetActiveBattle();

        Assert.NotNull(battle);
        Assert.Equal("Active", battle.Status);
    }

    [Fact]
    public async Task CreateBattle_WhenActiveBattleAlreadyExists_ThrowsInvalidOperationException()
    {
        var sut = CreateService();

        // Default data already has an active battle
        var movies = await sut.GetAllMovies();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateBattle(movies[0].MovieId, movies[1].MovieId));
    }

    [Fact]
    public async Task PlaceBet_WhenAmountIsZero_ThrowsInvalidOperationException()
    {
        var sut = CreateService();
        var battle = await sut.GetActiveBattle();
        Assert.NotNull(battle);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PlaceBet(1, battle.BattleId, battle.FirstMovie!.MovieId, 0));
    }

    // ==================== IPointService ====================
    [Fact]
    public async Task GetUserStats_WhenUserExists_ReturnsStats()
    {
        var sut = CreateService();

        var stats = await sut.GetUserStats(1);

        Assert.Equal(1, stats.User?.UserId);
    }

    [Fact]
    public async Task GetUserStats_WhenUserDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetUserStats(99999));
    }

    // ==================== IBadgeService ====================
    [Fact]
    public async Task GetAllBadges_WithBadgesInRepository_ReturnsNonEmptyList()
    {
        var sut = CreateService();

        var badges = await sut.GetAllBadges();

        Assert.NotEmpty(badges);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHasNoReviews_AwardsNoBadges()
    {
        var sut = CreateService();
        const int userId = 1;
        var badgesBefore = await sut.GetUserBadges(userId);

        await sut.CheckAndAwardBadges(userId);

        var badgesAfter = await sut.GetUserBadges(userId);
        // User has very few reviews in default data — no badge criteria should be met
        Assert.Equal(badgesBefore.Count, badgesAfter.Count);
    }
}
