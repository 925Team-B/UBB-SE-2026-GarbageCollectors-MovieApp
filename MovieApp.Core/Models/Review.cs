#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Represents a user's review of a movie, including optional extended review categories.
/// </summary>
public class Review
{
    /// <summary>Gets or sets the unique review identifier.</summary>
    public int ReviewId { get; set; }

    /// <summary>Gets or sets the star rating (0-5, 0.5 increments).</summary>
    public float StarRating { get; set; }

    /// <summary>Gets or sets the review content (max 5000 characters).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets whether this review has extended review data.</summary>
    public bool IsExtraReview { get; set; }

    // Extra review fields

    /// <summary>Gets or sets the cinematography rating (0-5).</summary>
    public int CinematographyRating { get; set; }

    /// <summary>Gets or sets the cinematography review text.</summary>
    public string? CinematographyText { get; set; }

    /// <summary>Gets or sets the acting rating (0-5).</summary>
    public int ActingRating { get; set; }

    /// <summary>Gets or sets the acting review text.</summary>
    public string? ActingText { get; set; }

    /// <summary>Gets or sets the CGI rating (0-5).</summary>
    public int CgiRating { get; set; }

    /// <summary>Gets or sets the CGI review text.</summary>
    public string? CgiText { get; set; }

    /// <summary>Gets or sets the plot rating (0-5).</summary>
    public int PlotRating { get; set; }

    /// <summary>Gets or sets the plot review text.</summary>
    public string? PlotText { get; set; }

    /// <summary>Gets or sets the sound rating (0-5).</summary>
    public int SoundRating { get; set; }

    /// <summary>Gets or sets the sound review text.</summary>
    public string? SoundText { get; set; }

    // Navigation properties

    /// <summary>Gets or sets the author user.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets the reviewed movie.</summary>
    public Movie? Movie { get; set; }

    public int UserDisplayId => User?.UserId ?? 0;
}
