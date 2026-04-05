#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for managing user points, scoring, and freezing/refunding.
/// </summary>
public class PointService : IPointService
{
    private readonly UserStatsRepository _userStatsRepository;
    private readonly UserRepository _userRepository;
    private readonly MovieRepository _movieRepository;
    private readonly IBadgeService _badgeService;

    /// <summary>
    /// Initializes a new instance of <see cref="PointService"/>.
    /// </summary>
    /// <param name="userStatsRepository">The user stats repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="movieRepository">The movie repository.</param>
    /// <param name="badgeService">The badge service for checking awards.</param>
    public PointService(
        UserStatsRepository userStatsRepository,
        UserRepository userRepository,
        MovieRepository movieRepository,
        IBadgeService badgeService)
    {
        _userStatsRepository = userStatsRepository;
        _userRepository = userRepository;
        _movieRepository = movieRepository;
        _badgeService = badgeService;
    }

    /// <summary>
    /// Gets a user's stats. Creates stats if they don't exist.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's stats.</returns>
    public async Task<UserStats> GetUserStats(int userId)
    {
        var stats = _userStatsRepository.GetByUserId(userId);

        if (stats == null)
        {
            var user = _userRepository.GetById(userId)
                ?? throw new InvalidOperationException("User not found.");

            stats = new UserStats
            {
                User = user,
                TotalPoints = 0,
                WeeklyScore = 0
            };
            _userStatsRepository.Insert(stats);
        }

        return stats;
    }

    /// <summary>
    /// Adds points based on movie rating and battle status.
    /// +2 if movie avg &gt; 3.5, +1 if movie avg &lt; 2.0, +5 if isBattleMovie.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="movieId">The movie identifier.</param>
    /// <param name="isBattleMovie">Whether the movie is in an active battle.</param>
    public async Task AddPoints(int userId, int movieId, bool isBattleMovie)
    {
        var stats = await GetUserStats(userId);
        var movie = _movieRepository.GetById(movieId);
        if (movie == null) return;

        int pointsToAdd = 0;

        if (movie.AverageRating > 3.5)
            pointsToAdd += 2;
        else if (movie.AverageRating < 2.0)
            pointsToAdd += 1;

        if (isBattleMovie)
            pointsToAdd += 5;

        stats.TotalPoints += pointsToAdd;
        if (stats.TotalPoints < 0) stats.TotalPoints = 0;

        _userStatsRepository.Update(stats);

        // Check for new badges
        await _badgeService.CheckAndAwardBadges(userId);
    }

    /// <summary>
    /// Deducts points from a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="points">Number of points to deduct.</param>
    public async Task DeductPoints(int userId, int points)
    {
        var stats = await GetUserStats(userId);
        stats.TotalPoints -= points;
        if (stats.TotalPoints < 0) stats.TotalPoints = 0;
        _userStatsRepository.Update(stats);
    }

    /// <summary>
    /// Freezes (deducts) points for a battle bet.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="amount">The amount to freeze.</param>
    /// <exception cref="InvalidOperationException">Thrown when insufficient points.</exception>
    public async Task FreezePoints(int userId, int amount)
    {
        var stats = await GetUserStats(userId);

        if (stats.TotalPoints < amount)
            throw new InvalidOperationException(
                $"Insufficient points. You have {stats.TotalPoints} but need {amount}.");

        stats.TotalPoints -= amount;
        _userStatsRepository.Update(stats);
    }

    /// <summary>
    /// Refunds previously frozen points back to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="amount">The amount to refund.</param>
    public async Task RefundPoints(int userId, int amount)
    {
        var stats = await GetUserStats(userId);
        stats.TotalPoints += amount;
        _userStatsRepository.Update(stats);
    }

    /// <summary>
    /// Updates the user's weekly score (resets weekly counter).
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    public async Task UpdateWeeklyScore(int userId)
    {
        var stats = await GetUserStats(userId);
        stats.WeeklyScore = stats.TotalPoints;
        _userStatsRepository.Update(stats);
    }
}
