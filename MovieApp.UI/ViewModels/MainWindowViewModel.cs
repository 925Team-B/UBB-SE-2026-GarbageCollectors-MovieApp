#nullable enable
using System.Windows.Input;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the main window, managing tab navigation and detail views.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly CatalogViewModel catalogViewModel;
    private readonly MovieDetailViewModel movieDetailViewModel;
    private readonly BattleViewModel battleViewModel;
    private readonly ForumViewModel forumViewModel;
    private readonly ProfileViewModel profileViewModel;

    private int selectedTabIndex;
    private bool showMovieDetail;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindowViewModel"/>.
    /// </summary>
    public MainWindowViewModel(CatalogViewModel catalogViewModel,
        MovieDetailViewModel movieDetailViewModel,
        BattleViewModel battleViewModel,
        ForumViewModel forumViewModel,
        ProfileViewModel profileViewModel)
    {
        this.catalogViewModel = catalogViewModel;
        this.movieDetailViewModel = movieDetailViewModel;
        this.battleViewModel = battleViewModel;
        this.forumViewModel = forumViewModel;
        this.profileViewModel = profileViewModel;

        // Wire up navigation events
        this.catalogViewModel.MovieSelected += OnMovieSelected;
        this.movieDetailViewModel.NavigateBack += OnNavigateBack;

        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
    }

    /// <summary>Gets the catalog view model.</summary>
    public CatalogViewModel CatalogViewModel => catalogViewModel;

    /// <summary>Gets the movie detail view model.</summary>
    public MovieDetailViewModel MovieDetailViewModel => movieDetailViewModel;

    /// <summary>Gets the battle view model.</summary>
    public BattleViewModel BattleViewModel => battleViewModel;

    /// <summary>Gets the forum view model.</summary>
    public ForumViewModel ForumViewModel => forumViewModel;

    /// <summary>Gets the profile view model.</summary>
    public ProfileViewModel ProfileViewModel => profileViewModel;

    /// <summary>Gets or sets the selected tab index.</summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => SetProperty(ref selectedTabIndex, value);
    }

    /// <summary>Gets or sets whether the movie detail view is showing.</summary>
    public bool ShowMovieDetail
    {
        get => showMovieDetail;
        set => SetProperty(ref showMovieDetail, value);
    }

    /// <summary>Gets the command to load initial data.</summary>
    public ICommand LoadDataCommand { get; }

    /// <summary>
    /// Loads initial data for all tabs.
    /// </summary>
    private async Task LoadDataAsync()
    {
        try
        {
            await catalogViewModel.LoadMoviesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Catalog load error] {ex.Message}");
        }
        try
        {
            await battleViewModel.LoadBattleAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Battle load error] {ex.Message}");
        }
        try
        {
            await forumViewModel.LoadMoviesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Forum load error] {ex.Message}");
        }
        try
        {
            await profileViewModel.LoadProfileAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Profile load error] {ex.Message}");
        }
    }

    /// <summary>
    /// Handles movie selection from the catalog, navigating to detail view.
    /// </summary>
    private async void OnMovieSelected(Movie movie)
    {
        ShowMovieDetail = true;
        await movieDetailViewModel.LoadMovieAsync(movie);
    }

    /// <summary>
    /// Handles navigation back from movie detail to catalog.
    /// Reloads catalog so updated average ratings are reflected immediately.
    /// </summary>
    private void OnNavigateBack()
    {
        ShowMovieDetail = false;
        _ = catalogViewModel.LoadMoviesAsync();
    }
}
