#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Represents a user's point statistics and rankings.
/// </summary>
public class UserStats
{
    /// <summary>Gets or sets the unique stats identifier.</summary>
    public int StatsId { get; set; }

    /// <summary>Gets or sets the user's total accumulated points.</summary>
    public int TotalPoints { get; set; }

    /// <summary>Gets or sets the user's weekly score.</summary>
    public int WeeklyScore { get; set; }

    // Navigation properties
    /// <summary>Gets or sets the associated user.</summary>
    public User? User { get; set; }
}
