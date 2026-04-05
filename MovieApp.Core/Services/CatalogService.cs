#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for movie catalog operations including search and filtering.
/// </summary>
public class CatalogService : ICatalogService
{
    private readonly IMovieRepository _movieRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="CatalogService"/>.
    /// </summary>
    /// <param name="movieRepository">The movie repository.</param>
    public CatalogService(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    /// <summary>
    /// Gets all movies in the catalog.
    /// </summary>
    /// <returns>A list of all movies.</returns>
    public async Task<List<Movie>> GetAllMovies()
    {
        var movies = _movieRepository.GetAll()
            .OrderBy(m => m.Title)
            .ToList();

        return await Task.FromResult(movies);
    }

    /// <summary>
    /// Gets a single movie by its ID.
    /// </summary>
    /// <param name="movieId">The movie identifier.</param>
    /// <returns>The movie if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when movie is not found.</exception>
    public async Task<Movie> GetMovieById(int movieId)
    {
        var movie = _movieRepository.GetById(movieId);
        return movie ?? throw new InvalidOperationException($"Movie with ID {movieId} not found.");
    }

    /// <summary>
    /// Searches movies by title containing the query (case-insensitive).
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A list of matching movies.</returns>
    public async Task<List<Movie>> SearchMovies(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllMovies();

        var movies = _movieRepository.GetAll()
            .Where(m => m.Title.ToLower().Contains(query.ToLower()))
            .OrderBy(m => m.Title)
            .ToList();

        return await Task.FromResult(movies);
    }

    /// <summary>
    /// Filters movies by genre and minimum average rating.
    /// </summary>
    /// <param name="genre">The genre to filter by.</param>
    /// <param name="minRating">The minimum average rating.</param>
    /// <returns>A list of filtered movies.</returns>
    public async Task<List<Movie>> FilterMovies(string genre, float minRating)
    {
        var query = _movieRepository.GetAll().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(m => m.Genre.ToLower() == genre.ToLower());
        }

        query = query.Where(m => m.AverageRating >= minRating);

        var movies = query
            .OrderBy(m => m.Title)
            .ToList();

        return await Task.FromResult(movies);
    }
}
