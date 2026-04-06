#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for point/score operations.
/// </summary>
public interface IPointService
{
    /// <summary>Gets a user's stats.</summary>
    Task<UserStats> GetUserStats(int userId);

    /// <summary>Adds points based on movie rating and battle status.</summary>
    Task AddPoints(int userId, int movieId, bool isBattleMovie);

    /// <summary>Deducts points from a user.</summary>
    Task DeductPoints(int userId, int points);

    /// <summary>Freezes (deducts) points for a bet.</summary>
    Task FreezePoints(int userId, int amount);

    /// <summary>Refunds frozen points back to a user.</summary>
    Task RefundPoints(int userId, int amount);

    /// <summary>Updates the user's weekly score.</summary>
    Task UpdateWeeklyScore(int userId);
}
