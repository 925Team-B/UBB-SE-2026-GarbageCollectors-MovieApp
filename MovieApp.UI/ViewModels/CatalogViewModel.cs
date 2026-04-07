#nullable enable
namespace MovieApp.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

/// <summary>
/// ViewModel for the movie catalog view with search and filter capabilities.
/// </summary>
public class CatalogViewModel : ViewModelBase
{
    private readonly ICatalogService catalogService;
    private string searchQuery = string.Empty;
    private string selectedGenre = "All Genres";
    private double minimumRating;
    private Movie? selectedMovie;

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
        this.catalogService = catalogService;

        // Commands
        this.SelectMovieCommand = new RelayCommand(param =>
        {
            if (param is Movie movie)
            {
                this.SelectedMovie = movie;
                this.MovieSelected?.Invoke(movie);
            }
        });

        this.LoadMoviesCommand = new AsyncRelayCommand(async _ => await this.LoadMoviesAsync());
        this.ClearFiltersCommand = new AsyncRelayCommand(async _ => await this.ClearFiltersAsync());
    }

    /// <summary>Gets the collection of movies to display.</summary>
    public ObservableCollection<Movie> Movies { get; } = new ();

    /// <summary>Gets the list of available genres.</summary>
    public ObservableCollection<string> Genres { get; } = new ()
    {
        "All Genres", "Action", "Comedy", "Crime", "Drama", "Sci-Fi"
    };

    /// <summary>Gets or sets the search query text.</summary>
    public string SearchQuery
    {
        get => this.searchQuery;
        set
        {
            if (this.SetProperty(ref this.searchQuery, value))
            {
                _ = this.UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the selected genre filter.</summary>
    public string SelectedGenre
    {
        get => this.selectedGenre;
        set
        {
            if (this.SetProperty(ref this.selectedGenre, value))
            {
                _ = this.UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the minimum rating filter.</summary>
    public double MinimumRating
    {
        get => this.minimumRating;
        set
        {
            if (this.SetProperty(ref this.minimumRating, value))
            {
                _ = this.UpdateResultsAsync();
            }
        }
    }

    /// <summary>Gets or sets the currently selected movie.</summary>
    public Movie? SelectedMovie
    {
        get => this.selectedMovie;
        set => this.SetProperty(ref this.selectedMovie, value);
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
        var movies = await this.catalogService.GetAllMovies();
        this.Movies.Clear();
        foreach (var movie in movies)
        {
            this.Movies.Add(movie);
        }
    }

    /// <summary>
    /// Unified method that applies Search, Genre, and Rating simultaneously.
    /// </summary>
    private async Task UpdateResultsAsync()
    {
        // 1. Get the base list of movies (either all, or by search query)
        IEnumerable<Movie> currentMovies;

        if (string.IsNullOrWhiteSpace(this.SearchQuery))
        {
            currentMovies = await this.catalogService.GetAllMovies();
        }
        else
        {
            currentMovies = await this.catalogService.SearchMovies(this.SearchQuery);
        }

        // 2. Apply the Genre filter in-memory if one is selected
        if (!string.IsNullOrWhiteSpace(this.SelectedGenre) && this.SelectedGenre != "All Genres")
        {
            currentMovies = currentMovies.Where(m => m.Genre == this.SelectedGenre);
        }

        // 3. Apply the Rating filter in-memory
        if (this.MinimumRating > 0)
        {
            currentMovies = currentMovies.Where(m => m.AverageRating >= this.MinimumRating);
        }

        // 4. Update the UI collection
        this.Movies.Clear();
        foreach (var movie in currentMovies)
        {
            this.Movies.Add(movie);
        }
    }

    /// <summary>
    /// Resets the search query, genre, and rating back to their defaults.
    /// </summary>
    private async Task ClearFiltersAsync()
    {
        // Temporarily disable the unified update so we don't trigger it 3 times in a row
        bool needsUpdate = false;

        if (!string.IsNullOrWhiteSpace(this.searchQuery))
        {
            this.SetProperty(ref this.searchQuery, string.Empty, nameof(this.SearchQuery));
            needsUpdate = true;
        }

        if (this.selectedGenre != "All Genres")
        {
            SetProperty(ref selectedGenre, "All Genres", nameof(this.SelectedGenre));
            needsUpdate = true;
        }

        if (this.minimumRating > 0)
        {
            this.SetProperty(ref this.minimumRating, 0, nameof(this.MinimumRating));
            needsUpdate = true;
        }

        // Only hit the database/update UI once after all properties are reset
        if (needsUpdate)
        {
            await this.UpdateResultsAsync();
        }
    }
}