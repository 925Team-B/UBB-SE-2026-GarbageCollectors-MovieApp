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
    private readonly MainWindowViewModel viewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="MainWindow"/>.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();

        viewModel = App.Services.GetRequiredService<MainWindowViewModel>();

        // Set DataContext for child views
        CatalogViewControl.DataContext = viewModel.CatalogViewModel;
        MovieDetailViewControl.DataContext = viewModel.MovieDetailViewModel;
        BattleViewControl.DataContext = viewModel.BattleViewModel;
        ForumViewControl.DataContext = viewModel.ForumViewModel;
        ProfileViewControl.DataContext = viewModel.ProfileViewModel;

        // Wire up detail view navigation
        viewModel.CatalogViewModel.MovieSelected += movie =>
        {
            MovieDetailOverlay.Visibility = Visibility.Visible;
        };

        viewModel.MovieDetailViewModel.NavigateBack += () =>
        {
            MovieDetailOverlay.Visibility = Visibility.Collapsed;
        };

        this.Title = App.UsingMockData ? "MovieApp [MOCK DATA]" : "MovieApp [DATABASE]";

        // Load initial data
        this.Activated += async (s, e) =>
        {
            viewModel.LoadDataCommand.Execute(null);
        };
    }
}
