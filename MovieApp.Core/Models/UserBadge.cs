#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Junction table for User-Badge many-to-many relationship. Composite PK: (UserId, BadgeId).
/// </summary>
public class UserBadge
{
    // Navigation properties
    /// <summary>Gets or sets the associated user.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the associated badge.</summary>
    public Badge? Badge { get; set; }
}
