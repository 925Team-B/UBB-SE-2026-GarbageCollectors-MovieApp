#nullable enable
using System.Windows.Input;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the main window, managing tab navigation and detail views.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly CatalogViewModel _catalogViewModel;
    private readonly MovieDetailViewModel _movieDetailViewModel;
    private readonly BattleViewModel _battleViewModel;
    private readonly ForumViewModel _forumViewModel;
    private readonly ProfileViewModel _profileViewModel;

    private int _selectedTabIndex;
    private bool _showMovieDetail;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindowViewModel"/>.
    /// </summary>
    public MainWindowViewModel(CatalogViewModel catalogViewModel,
        MovieDetailViewModel movieDetailViewModel,
        BattleViewModel battleViewModel,
        ForumViewModel forumViewModel,
        ProfileViewModel profileViewModel)
    {
        _catalogViewModel = catalogViewModel;
        _movieDetailViewModel = movieDetailViewModel;
        _battleViewModel = battleViewModel;
        _forumViewModel = forumViewModel;
        _profileViewModel = profileViewModel;

        // Wire up navigation events
        _catalogViewModel.MovieSelected += OnMovieSelected;
        _movieDetailViewModel.NavigateBack += OnNavigateBack;

        LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
    }

    /// <summary>Gets the catalog view model.</summary>
    public CatalogViewModel CatalogViewModel => _catalogViewModel;

    /// <summary>Gets the movie detail view model.</summary>
    public MovieDetailViewModel MovieDetailViewModel => _movieDetailViewModel;

    /// <summary>Gets the battle view model.</summary>
    public BattleViewModel BattleViewModel => _battleViewModel;

    /// <summary>Gets the forum view model.</summary>
    public ForumViewModel ForumViewModel => _forumViewModel;

    /// <summary>Gets the profile view model.</summary>
    public ProfileViewModel ProfileViewModel => _profileViewModel;

    /// <summary>Gets or sets the selected tab index.</summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    /// <summary>Gets or sets whether the movie detail view is showing.</summary>
    public bool ShowMovieDetail
    {
        get => _showMovieDetail;
        set => SetProperty(ref _showMovieDetail, value);
    }

    /// <summary>Gets the command to load initial data.</summary>
    public ICommand LoadDataCommand { get; }

    /// <summary>
    /// Loads initial data for all tabs.
    /// </summary>
    private async Task LoadDataAsync()
    {
        try { await _catalogViewModel.LoadMoviesAsync(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Catalog load error] {ex.Message}"); }
        try { await _battleViewModel.LoadBattleAsync(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Battle load error] {ex.Message}"); }
        try { await _forumViewModel.LoadMoviesAsync(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Forum load error] {ex.Message}"); }
        try { await _profileViewModel.LoadProfileAsync(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Profile load error] {ex.Message}"); }
    }

    /// <summary>
    /// Handles movie selection from the catalog, navigating to detail view.
    /// </summary>
    private async void OnMovieSelected(Movie movie)
    {
        ShowMovieDetail = true;
        await _movieDetailViewModel.LoadMovieAsync(movie);
    }

    /// <summary>
    /// Handles navigation back from movie detail to catalog.
    /// Reloads catalog so updated average ratings are reflected immediately.
    /// </summary>
    private void OnNavigateBack()
    {
        ShowMovieDetail = false;
        _ = _catalogViewModel.LoadMoviesAsync();
    }
}
