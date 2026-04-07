#nullable enable
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI.Views;

/// <summary>
/// Movie detail view showing full movie information, reviews, and comments.
/// </summary>
public sealed partial class MovieDetailView : UserControl
{
    /// <summary>Gets the ViewModel.</summary>
    public MovieDetailViewModel? ViewModel => DataContext as MovieDetailViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="MovieDetailView"/>.
    /// </summary>
    public MovieDetailView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (s, e) => Bindings.Update();
    }

    private void ReplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not int commentId || ViewModel == null)
        {
            return;
        }

        ViewModel.StartReplyCommand.Execute(commentId);
        ReplyEditorBorder.Visibility = Visibility.Visible; // The line that was crashing!

        DispatcherQueue.TryEnqueue(() =>
        {
            ReplyEditorBorder.UpdateLayout();
            ReplyEditorBorder.StartBringIntoView();
        });
    }

    private void CancelReply_Click(object sender, RoutedEventArgs e)
    {
        ReplyEditorBorder.Visibility = Visibility.Collapsed;
    }
}
