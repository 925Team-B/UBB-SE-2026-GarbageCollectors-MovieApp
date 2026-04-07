#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for badge/achievement management and awarding.
/// </summary>
public class BadgeService : IBadgeService
{
    private readonly IUserBadgeRepository userBadgeRepository;
    private readonly IBadgeRepository badgeRepository;
    private readonly IReviewRepository reviewRepository;
    private readonly IMovieRepository movieRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="BadgeService"/>.
    /// </summary>
    /// <param name="userBadgeRepository">The user badge repository.</param>
    /// <param name="badgeRepository">The badge repository.</param>
    /// <param name="reviewRepository">The review repository.</param>
    /// <param name="movieRepository">The movie repository.</param>
    public BadgeService(
        IUserBadgeRepository userBadgeRepository,
        IBadgeRepository badgeRepository,
        IReviewRepository reviewRepository,
        IMovieRepository movieRepository)
    {
        this.userBadgeRepository = userBadgeRepository;
        this.badgeRepository = badgeRepository;
        this.reviewRepository = reviewRepository;
        this.movieRepository = movieRepository;
    }

    /// <summary>
    /// Gets all badges earned by a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of badges the user has earned.</returns>
    public async Task<List<Badge>> GetUserBadges(int userId)
    {
        var badges = userBadgeRepository.GetAll()
            .Where(ub => ub.User?.UserId == userId && ub.Badge is not null)
            .Select(ub => ub.Badge!)
            .ToList();

        return await Task.FromResult(badges);
    }

    /// <summary>
    /// Gets all available badges in the system.
    /// </summary>
    /// <returns>A list of all badges.</returns>
    public async Task<List<Badge>> GetAllBadges()
    {
        return await Task.FromResult(badgeRepository.GetAll());
    }

    /// <summary>
    /// Checks all badge criteria and awards any newly earned badges to the user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    public async Task CheckAndAwardBadges(int userId)
    {
        var existingBadgeIds = userBadgeRepository.GetAll()
            .Where(ub => ub.User?.UserId == userId && ub.Badge is not null)
            .Select(ub => ub.Badge!.BadgeId)
            .ToList();

        var allBadges = badgeRepository.GetAll();

        var userReviews = reviewRepository.GetAll()
            .Where(r => r.User?.UserId == userId)
            .ToList();
        var moviesById = movieRepository.GetAll().ToDictionary(m => m.MovieId);

        int totalReviews = userReviews.Count;
        int extraReviews = userReviews.Count(r => r.IsExtraReview);

        // Count reviews where all extra fields are completed
        int fullyCompletedExtraReviews = userReviews.Count(r =>
            r.IsExtraReview &&
            !string.IsNullOrEmpty(r.CinematographyText) &&
            !string.IsNullOrEmpty(r.ActingText) &&
            !string.IsNullOrEmpty(r.CgiText) &&
            !string.IsNullOrEmpty(r.PlotText) &&
            !string.IsNullOrEmpty(r.SoundText));

        // Count comedy genre reviews
        int comedyReviews = userReviews.Count(r =>
            r.Movie is not null &&
            moviesById.TryGetValue(r.Movie.MovieId, out var movie) &&
            movie.Genre.Equals("Comedy", StringComparison.OrdinalIgnoreCase));
        double comedyPercentage = totalReviews > 0 ? (double)comedyReviews / totalReviews * 100 : 0;

        foreach (var badge in allBadges)
        {
            if (existingBadgeIds.Contains(badge.BadgeId))
            {
                continue;
            }

            bool shouldAward = badge.Name switch
            {
                "The Snob" => extraReviews >= 10,
                "Why so serious?" => fullyCompletedExtraReviews >= 50,
                "The Joker" => comedyPercentage > 70,
                "The Godfather I" => totalReviews >= 100,
                "The Godfather II" => totalReviews >= 200,
                "The Godfather III" => totalReviews >= 300,
                _ => false
            };

            if (shouldAward)
            {
                userBadgeRepository.Insert(new UserBadge
                {
                    User = new User { UserId = userId },
                    Badge = new Badge { BadgeId = badge.BadgeId }
                });
            }
        }

        await Task.CompletedTask;
    }
}
