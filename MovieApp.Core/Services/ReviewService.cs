#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for review operations including CRUD and extended reviews.
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBattleRepository _battleRepository;
    private readonly IPointService _pointService;

    /// <summary>
    /// Initializes a new instance of <see cref="ReviewService"/>.
    /// </summary>
    /// <param name="reviewRepository">The review repository.</param>
    /// <param name="movieRepository">The movie repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="battleRepository">The battle repository.</param>
    /// <param name="pointService">The point service for awarding points.</param>
    public ReviewService(
        ReviewRepository reviewRepository,
        MovieRepository movieRepository,
        UserRepository userRepository,
        BattleRepository battleRepository,
        IPointService pointService)
    {
        _reviewRepository = reviewRepository;
        _movieRepository = movieRepository;
        _userRepository = userRepository;
        _battleRepository = battleRepository;
        _pointService = pointService;
    }

    /// <summary>
    /// Gets all reviews for a specific movie.
    /// </summary>
    /// <param name="movieId">The movie identifier.</param>
    /// <returns>A list of reviews for the movie.</returns>
    public async Task<List<Review>> GetReviewsForMovie(int movieId)
    {
        var reviews = _reviewRepository.GetAll()
            .Where(r => r.Movie?.MovieId == movieId && r.StarRating <= 5)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return await Task.FromResult(reviews);
    }

    /// <summary>
    /// Adds a new review for a movie. Validates uniqueness and rating range.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="movieId">The movie's ID.</param>
    /// <param name="rating">Star rating (0-5, 0.5 increments).</param>
    /// <param name="content">Review content (max 2000 chars, at least 50 chars).</param>
    /// <returns>The created review.</returns>
    /// <exception cref="InvalidOperationException">Thrown on duplicate review or invalid input.</exception>
    public async Task<Review> AddReview(int userId, int movieId, float rating, string content)
    {
        var user = _userRepository.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");
        var movie = _movieRepository.GetById(movieId)
            ?? throw new InvalidOperationException("Movie not found.");

        // Validate: one review per user per movie
        var existing = _reviewRepository.GetAll()
            .Any(r => r.User?.UserId == userId && r.Movie?.MovieId == movieId);
        if (existing)
            throw new InvalidOperationException("User has already reviewed this movie.");

        // Validate rating range (0-5, 0.5 increments)
        if (rating < 0 || rating > 5 || (rating * 2) % 1 != 0)
            throw new InvalidOperationException("Rating must be between 0 and 5 in 0.5 increments.");

        // Validate content length
        if (!string.IsNullOrEmpty(content) && content.Length > 2000)
            throw new InvalidOperationException("Review content must not exceed 2000 characters.");


        var review = new Review
        {
            User = user,
            Movie = movie,
            StarRating = rating,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsExtraReview = false
        };

        _reviewRepository.Insert(review);

        // Recalculate average rating
        await RecalculateAverageRating(movieId);

        // Determine if movie is in an active battle
        var isBattleMovie = _battleRepository.GetAll()
            .Any(b => b.Status == "Active" &&
                      (b.FirstMovie?.MovieId == movieId || b.SecondMovie?.MovieId == movieId));

        // Award points
        await _pointService.AddPoints(userId, movieId, isBattleMovie);

        return review;
    }

    /// <summary>
    /// Updates an existing review's rating and content.
    /// </summary>
    /// <param name="reviewId">The review identifier.</param>
    /// <param name="rating">The new star rating.</param>
    /// <param name="content">The new content.</param>
    public async Task UpdateReview(int reviewId, float rating, string content)
    {
        var review = _reviewRepository.GetById(reviewId)
            ?? throw new InvalidOperationException("Review not found.");

        if (rating < 0 || rating > 5 || (rating * 2) % 1 != 0)
            throw new InvalidOperationException("Rating must be between 0 and 5 in 0.5 increments.");

        if (!string.IsNullOrEmpty(content) && content.Length > 2000)
            throw new InvalidOperationException("Review content must not exceed 2000 characters.");

        review.StarRating = rating;
        review.Content = content;
        _reviewRepository.Update(review);

        var movieId = review.Movie?.MovieId
            ?? throw new InvalidOperationException("Review movie is not available.");
        await RecalculateAverageRating(movieId);
    }

    /// <summary>
    /// Deletes a review by its ID.
    /// </summary>
    /// <param name="reviewId">The review identifier.</param>
    public async Task DeleteReview(int reviewId)
    {
        var review = _reviewRepository.GetById(reviewId)
            ?? throw new InvalidOperationException("Review not found.");

        int movieId = review.Movie?.MovieId
            ?? throw new InvalidOperationException("Review movie is not available.");
        _reviewRepository.Delete(review.ReviewId);

        await RecalculateAverageRating(movieId);
    }

    /// <summary>
    /// Submits extended review data with category-specific ratings and text.
    /// </summary>
    /// <param name="reviewId">The review to extend.</param>
    /// <param name="cgRating">CGI rating (0-5).</param>
    /// <param name="cgText">CGI review text (50-2000 chars).</param>
    /// <param name="actingRating">Acting rating (0-5).</param>
    /// <param name="actingText">Acting review text (50-2000 chars).</param>
    /// <param name="plotRating">Plot rating (0-5).</param>
    /// <param name="plotText">Plot review text (50-2000 chars).</param>
    /// <param name="soundRating">Sound rating (0-5).</param>
    /// <param name="soundText">Sound review text (50-2000 chars).</param>
    /// <param name="cinRating">Cinematography rating (0-5).</param>
    /// <param name="cinText">Cinematography review text (50-2000 chars).</param>
    /// <param name="mainExtraText">Main extended review text (500-12000 chars).</param>
    public async Task SubmitExtraReview(int reviewId, int cgRating, string cgText,
        int actingRating, string actingText, int plotRating, string plotText,
        int soundRating, string soundText, int cinRating, string cinText,
        string mainExtraText)
    {
        var review = _reviewRepository.GetById(reviewId)
            ?? throw new InvalidOperationException("Review not found.");

        // Validate main extra text length
        if (string.IsNullOrEmpty(mainExtraText) || mainExtraText.Length < 500 || mainExtraText.Length > 12000)
            throw new InvalidOperationException("Main extra text must be between 500 and 12000 characters.");

        // Validate category texts
        ValidateCategoryText(cgText, "CGI");
        ValidateCategoryText(actingText, "Acting");
        ValidateCategoryText(plotText, "Plot");
        ValidateCategoryText(soundText, "Sound");
        ValidateCategoryText(cinText, "Cinematography");

        // Validate category ratings (0-5)
        ValidateCategoryRating(cgRating, "CGI");
        ValidateCategoryRating(actingRating, "Acting");
        ValidateCategoryRating(plotRating, "Plot");
        ValidateCategoryRating(soundRating, "Sound");
        ValidateCategoryRating(cinRating, "Cinematography");

        review.CgiRating = cgRating;
        review.CgiText = cgText;
        review.ActingRating = actingRating;
        review.ActingText = actingText;
        review.PlotRating = plotRating;
        review.PlotText = plotText;
        review.SoundRating = soundRating;
        review.SoundText = soundText;
        review.CinematographyRating = cinRating;
        review.CinematographyText = cinText;
        review.Content = mainExtraText;
        review.IsExtraReview = true;

        _reviewRepository.Update(review);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the average rating for a movie.
    /// </summary>
    /// <param name="movieId">The movie identifier.</param>
    /// <returns>The average star rating.</returns>
    public async Task<double> GetAverageRating(int movieId)
    {
        var reviews = _reviewRepository.GetAll()
            .Where(r => r.Movie?.MovieId == movieId && r.StarRating <= 5)
            .ToList();

        if (reviews.Count == 0)
            return 0;

        return Math.Round(reviews.Average(r => r.StarRating), 1);
    }

    /// <summary>
    /// Recalculates and updates the average rating for a movie.
    /// </summary>
    private async Task RecalculateAverageRating(int movieId)
    {
        var movie = _movieRepository.GetById(movieId);
        if (movie == null) return;

        var avg = await GetAverageRating(movieId);
        movie.AverageRating = avg;
        _movieRepository.Update(movie);
    }

    /// <summary>Validates a category text's length (50-2000 chars).</summary>
    private static void ValidateCategoryText(string text, string categoryName)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 50 || text.Length > 2000)
            throw new InvalidOperationException(
                $"{categoryName} text must be between 50 and 2000 characters.");
    }

    /// <summary>Validates a category rating is between 0 and 5.</summary>
    private static void ValidateCategoryRating(int rating, string categoryName)
    {
        if (rating < 0 || rating > 5)
            throw new InvalidOperationException(
                $"{categoryName} rating must be between 0 and 5.");
    }
}
