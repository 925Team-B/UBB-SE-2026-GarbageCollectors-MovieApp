#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for battle/betting operations.
/// </summary>
public interface IBattleService
{
    /// <summary>Gets the currently active battle.</summary>
    Task<Battle?> GetActiveBattle();

    /// <summary>Creates a new battle between two movies.</summary>
    Task<Battle> CreateBattle(int firstMovieId, int secondMovieId);

    /// <summary>Places a bet on a battle.</summary>
    Task<Bet> PlaceBet(int userId, int battleId, int movieId, int amount);

    /// <summary>Gets a user's bet for a specific battle.</summary>
    Task<Bet?> GetBet(int userId, int battleId);

    /// <summary>Determines the winning movie of a battle.</summary>
    Task<int> DetermineWinner(int battleId);

    /// <summary>Distributes payouts to winning bettors.</summary>
    Task DistributePayouts(int battleId);

    /// <summary>Settles any active battles whose end date has passed.</summary>
    Task SettleExpiredBattlesAsync();

    /// <summary>
    /// Gets the active battle, or the most recent battle the user has bet on if no active battle exists.
    /// </summary>
    Task<Battle?> GetCurrentBattleForUser(int userId);

    // ── Demo helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Immediately settles the given battle regardless of its end date.
    /// Determines the winner, distributes payouts, and marks the battle Finished.
    /// Use this for demo / testing purposes.
    /// </summary>
    Task ForceSettleBattleAsync(int battleId);

    /// <summary>
    /// Deletes every battle and its bets from the database and refunds any
    /// still-frozen points to the affected users.
    /// Use this to reset the demo to a clean slate.
    /// </summary>
    Task ResetAllBattlesForDemoAsync();

    /// <summary>
    /// Picks two movies with similar ratings and creates a new active battle
    /// starting today.  Throws if no suitable pair exists.
    /// </summary>
    Task<Battle> CreateDemoBattleAsync();
}
