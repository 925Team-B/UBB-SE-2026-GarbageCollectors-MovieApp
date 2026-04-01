#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for review operations.
/// </summary>
public interface IReviewService
{
    /// <summary>Gets all reviews for a specific movie.</summary>
    Task<List<Review>> GetReviewsForMovie(int movieId);

    /// <summary>Adds a new review for a movie.</summary>
    Task<Review> AddReview(int userId, int movieId, float rating, string content);

    /// <summary>Updates an existing review.</summary>
    Task UpdateReview(int reviewId, float rating, string content);

    /// <summary>Deletes a review.</summary>
    Task DeleteReview(int reviewId);

    /// <summary>Submits an extended review with category ratings.</summary>
    Task SubmitExtraReview(int reviewId, int cgRating, string cgText,
        int actingRating, string actingText, int plotRating, string plotText,
        int soundRating, string soundText, int cinRating, string cinText,
        string mainExtraText);

    /// <summary>Gets the average rating for a movie.</summary>
    Task<double> GetAverageRating(int movieId);
}
