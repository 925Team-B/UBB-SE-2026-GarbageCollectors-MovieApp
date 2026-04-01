#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Represents a user's bet on a movie battle. Composite PK: (UserId, BattleId).
/// </summary>
public class Bet
{
    /// <summary>Gets or sets the amount of points bet.</summary>
    public int Amount { get; set; }

    // Navigation properties
    /// <summary>Gets or sets the betting user.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the battle.</summary>
    public Battle? Battle { get; set; }

    /// <summary>Gets or sets the movie being bet on.</summary>
    public Movie? Movie { get; set; }
}
