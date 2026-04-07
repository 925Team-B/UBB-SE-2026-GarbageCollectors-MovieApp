#nullable enable

namespace MovieApp.Core.Models;

/// <summary>
/// Represents a movie in the catalog.
/// </summary>
public class Movie
{
    /// <summary>Gets or sets the unique movie identifier.</summary>
    public int MovieId { get; set; }

    /// <summary>Gets or sets the movie title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the release year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the poster image URL.</summary>
    public string PosterUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the movie genre.</summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculated average rating.</summary>
    public double AverageRating { get; set; }

    // Navigation properties

    /// <summary>Gets or sets the collection of reviews for this movie.</summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Gets or sets the collection of comments for this movie.</summary>
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public override string ToString() => Title;
}
