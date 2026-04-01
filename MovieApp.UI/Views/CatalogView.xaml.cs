#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MovieApp.Core.Models;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI.Views;

/// <summary>
/// Catalog view displaying all movies with search and filter.
/// </summary>
public sealed partial class CatalogView : UserControl
{
    /// <summary>Gets the ViewModel.</summary>
    public CatalogViewModel? ViewModel => DataContext as CatalogViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="CatalogView"/>.
    /// </summary>
    public CatalogView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (s, e) => Bindings.Update();
    }

    /// <summary>
    /// Handles movie item click to navigate to detail view.
    /// </summary>
    private void MovieList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Movie movie)
        {
            ViewModel?.SelectMovieCommand.Execute(movie);
        }
    }
}
