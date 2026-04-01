#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI;

/// <summary>
/// Main application window with tab navigation.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();

        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();

        // Set DataContext for child views
        CatalogViewControl.DataContext = _viewModel.CatalogViewModel;
        MovieDetailViewControl.DataContext = _viewModel.MovieDetailViewModel;
        BattleViewControl.DataContext = _viewModel.BattleViewModel;
        ForumViewControl.DataContext = _viewModel.ForumViewModel;
        ProfileViewControl.DataContext = _viewModel.ProfileViewModel;

        // Wire up detail view navigation
        _viewModel.CatalogViewModel.MovieSelected += movie =>
        {
            MovieDetailOverlay.Visibility = Visibility.Visible;
        };

        _viewModel.MovieDetailViewModel.NavigateBack += () =>
        {
            MovieDetailOverlay.Visibility = Visibility.Collapsed;
        };

        this.Title = App.UsingMockData ? "MovieApp [MOCK DATA]" : "MovieApp [DATABASE]";

        // Load initial data
        this.Activated += async (s, e) =>
        {
            _viewModel.LoadDataCommand.Execute(null);
        };
    }
}
