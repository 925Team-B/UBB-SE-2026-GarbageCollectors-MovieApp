#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MovieApp.Core.Models;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI.Views;

/// <summary>
/// Forum view showing threaded comments for movies.
/// </summary>
public sealed partial class ForumView : UserControl
{
    /// <summary>Gets the ViewModel.</summary>
    public ForumViewModel? ViewModel => DataContext as ForumViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="ForumView"/>.
    /// </summary>
    public ForumView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (s, e) => this.Bindings.Update();
    }

    /// <summary>
    /// Handles reply button click on a comment.
    /// </summary>
    private void ReplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.Tag is int commentId && ViewModel != null)
        {
            ViewModel.ReplyToCommentId = commentId;
        }
    }
}
