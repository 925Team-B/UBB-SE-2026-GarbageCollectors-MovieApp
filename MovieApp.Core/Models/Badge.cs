#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Represents a badge (achievement) that can be earned by users.
/// </summary>
public class Badge
{
    /// <summary>Gets or sets the unique badge identifier.</summary>
    public int BadgeId { get; set; }

    /// <summary>Gets or sets the badge name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the criteria value for earning this badge.</summary>
    public int CriteriaValue { get; set; }

    // Navigation properties

    /// <summary>Gets or sets the collection of user-badge associations.</summary>
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
