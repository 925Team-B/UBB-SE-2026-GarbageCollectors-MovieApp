#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// DTO representing an external critic review from third-party sources.
/// Not stored in the database.
/// </summary>
public class CriticReview
{
    /// <summary>Gets or sets the review source (e.g., NYT, Guardian, OMDb).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the critic's score.</summary>
    public double Score { get; set; }

    /// <summary>Gets a display-friendly score string; shows "N/A" when the source does not provide a numeric score.</summary>
    public string ScoreDisplay => Score > 0 ? Score.ToString("F1") : string.Empty;

    /// <summary>True when this source provides a numeric score.</summary>
    public bool HasScore => Score > 0;

    /// <summary>Gets or sets the review headline.</summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Gets or sets a short snippet from the review.</summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL to the full review.</summary>
    public string Url { get; set; } = string.Empty;
}
