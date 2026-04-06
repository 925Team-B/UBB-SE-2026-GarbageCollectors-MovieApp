#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for movie catalog operations.
/// </summary>
public interface ICatalogService
{
    /// <summary>Gets all movies in the catalog.</summary>
    Task<List<Movie>> GetAllMovies();

    /// <summary>Gets a single movie by its ID.</summary>
    Task<Movie> GetMovieById(int movieId);

    /// <summary>Searches movies by title (case-insensitive).</summary>
    Task<List<Movie>> SearchMovies(string query);

    /// <summary>Filters movies by genre and minimum rating.</summary>
    Task<List<Movie>> FilterMovies(string genre, float minRating);
}
