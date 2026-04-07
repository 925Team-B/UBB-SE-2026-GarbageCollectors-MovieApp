#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// Wraps a badge with its locked/unlocked state for display on the profile page.
/// </summary>
public sealed class BadgeDisplayItem
{
    public Badge Badge { get; }

    public bool IsUnlocked { get; }

    public string Name => this.Badge.Name;

    public int CriteriaValue => this.Badge.CriteriaValue;

    /// <summary>Medal emoji when unlocked, padlock when locked.</summary>
    public string Icon => IsUnlocked ? "🏅" : "🔒";

    /// <summary>Human-readable criteria description for each known badge.</summary>
    public string CriteriaDescription => this.Badge.Name switch
    {
        "The Snob" => $"Write {this.Badge.CriteriaValue} extra reviews",
        "Why so serious?" => $"Fully complete {this.Badge.CriteriaValue} extra reviews",
        "The Joker" => $"70%+ of your reviews on Comedy movies",
        "The Godfather I" => $"Write {this.Badge.CriteriaValue} total reviews",
        "The Godfather II" => $"Write {this.Badge.CriteriaValue} total reviews",
        "The Godfather III" => $"Write {this.Badge.CriteriaValue} total reviews",
        _ => $"Criteria value: {this.Badge.CriteriaValue}"
    };

    public BadgeDisplayItem(Badge badge, bool isUnlocked)
    {
        this.Badge = badge;
        this.IsUnlocked = isUnlocked;
    }
}
