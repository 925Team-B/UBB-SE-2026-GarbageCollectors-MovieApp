#nullable enable
using Microsoft.UI.Xaml.Controls;
using MovieApp.Core.Models;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI.Views;

/// <summary>
/// Battle arena view for movie battles and betting.
/// </summary>
public sealed partial class BattleView : UserControl
{
    /// <summary>Gets the ViewModel.</summary>
    public BattleViewModel? ViewModel => DataContext as BattleViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="BattleView"/>.
    /// </summary>
    public BattleView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (s, e) => Bindings.Update();
    }

    /// <summary>
    /// Handles movie selection in the bet form ComboBox.
    /// </summary>
    private void BetMovieSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BetMovieSelector.SelectedItem is Movie movie && ViewModel != null)
        {
            ViewModel.SelectedBetMovieId = movie.MovieId;
        }
    }
}
