#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for badge/achievement operations.
/// </summary>
public interface IBadgeService
{
    /// <summary>Gets all badges earned by a user.</summary>
    Task<List<Badge>> GetUserBadges(int userId);

    /// <summary>Gets all available badges.</summary>
    Task<List<Badge>> GetAllBadges();

    /// <summary>Checks and awards any newly earned badges to a user.</summary>
    Task CheckAndAwardBadges(int userId);
}
