#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the movie catalog view with search and filter capabilities.
/// </summary>
public class CatalogViewModel : ViewModelBase
{
    private readonly ICatalogService _catalogService;
    private string _searchQuery = string.Empty;
    private string _selectedGenre = "All";
    private double _minimumRating;
    private Movie? _selectedMovie;

    /// <summary>
    /// Event raised when a movie is selected for detail view.
    /// </summary>
    public event Action<Movie>? MovieSelected;

    /// <summary>
    /// Initializes a new instance of <see cref="CatalogViewModel"/>.
    /// </summary>
    /// <param name="catalogService">The catalog service.</param>
    public CatalogViewModel(ICatalogService catalogService)
    {
        _catalogService = catalogService;

        // Commands
        SelectMovieCommand = new RelayCommand(param =>
        {
            if (param is Movie movie)
            {
                SelectedMovie = movie;
                MovieSelected?.Invoke(movie);
            }
        });

        LoadMoviesCommand = new AsyncRelayCommand(async _ => await LoadMoviesAsync());
        ClearFiltersCommand = new AsyncRelayCommand(async _ => await ClearFiltersAsync());
    }

    /// <summary>Gets the collection of movies to display.</summary>
    public ObservableCollection<Movie> Movies { get; } = new();

    /// <summary>Gets the list of available genres.</summary>
    public ObservableCollection<string> Genres { get; } = new()
    {
        "All Genres", "Action", "Comedy", "Crime", "Drama", "Sci-Fi"
    };

    /// <summary>Gets or sets the search query text.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the selected genre filter.</summary>
    public string SelectedGenre
    {
        get => _selectedGenre;
        set
        {
            if (SetProperty(ref _selectedGenre, value))
            {
                _ = UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the minimum rating filter.</summary>
    public double MinimumRating
    {
        get => _minimumRating;
        set
        {
            if (SetProperty(ref _minimumRating, value))
            {
                _ = UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the currently selected movie.</summary>
    public Movie? SelectedMovie
    {
        get => _selectedMovie;
        set => SetProperty(ref _selectedMovie, value);
    }

    /// <summary>Gets the command to select a movie.</summary>
    public ICommand SelectMovieCommand { get; }

    /// <summary>Gets the command to load all movies initially.</summary>
    public ICommand LoadMoviesCommand { get; }

    /// <summary>Gets the command to clear all active filters and search.</summary>
    public ICommand ClearFiltersCommand { get; }

    /// <summary>
    /// Loads all movies from the catalog (usually called when the page first loads).
    /// </summary>
    public async Task LoadMoviesAsync()
    {
        var movies = await _catalogService.GetAllMovies();
        Movies.Clear();
        foreach (var movie in movies)
        {
            Movies.Add(movie);
        }
    }

    /// <summary>
    /// Unified method that applies Search, Genre, and Rating simultaneously.
    /// </summary>
    private async Task UpdateResultsAsync()
    {
        // 1. Get the base list of movies (either all, or by search query)
        IEnumerable<Movie> currentMovies;

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            currentMovies = await _catalogService.GetAllMovies();
        }
        else
        {
            currentMovies = await _catalogService.SearchMovies(SearchQuery);
        }

        // 2. Apply the Genre filter in-memory if one is selected
        if (!string.IsNullOrWhiteSpace(SelectedGenre) && SelectedGenre != "All Genres")
        {
            currentMovies = currentMovies.Where(m => m.Genre == SelectedGenre);
        }

        // 3. Apply the Rating filter in-memory
        if (MinimumRating > 0)
        {
            currentMovies = currentMovies.Where(m => m.AverageRating >= MinimumRating);
        }

        // 4. Update the UI collection
        Movies.Clear();
        foreach (var movie in currentMovies)
        {
            Movies.Add(movie);
        }
    }

    /// <summary>
    /// Resets the search query, genre, and rating back to their defaults.
    /// </summary>
    private async Task ClearFiltersAsync()
    {
        // Temporarily disable the unified update so we don't trigger it 3 times in a row
        bool needsUpdate = false;

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            SetProperty(ref _searchQuery, string.Empty, nameof(SearchQuery));
            needsUpdate = true;
        }

        if (_selectedGenre != "All Genres")
        {
            SetProperty(ref _selectedGenre, "All Genres", nameof(SelectedGenre));
            needsUpdate = true;
        }

        if (_minimumRating > 0)
        {
            SetProperty(ref _minimumRating, 0, nameof(MinimumRating));
            needsUpdate = true;
        }

        // Only hit the database/update UI once after all properties are reset
        if (needsUpdate)
        {
            await UpdateResultsAsync();
        }
    }
}