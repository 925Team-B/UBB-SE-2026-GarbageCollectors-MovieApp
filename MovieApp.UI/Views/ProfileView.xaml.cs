#nullable enable
using Microsoft.UI.Xaml.Controls;
using MovieApp.UI.ViewModels;

namespace MovieApp.UI.Views;

/// <summary>
/// User profile view showing points and earned badges.
/// </summary>
public sealed partial class ProfileView : UserControl
{
    /// <summary>Gets the ViewModel.</summary>
    public ProfileViewModel? ViewModel => DataContext as ProfileViewModel;

    /// <summary>
    /// Initializes a new instance of <see cref="ProfileView"/>.
    /// </summary>
    public ProfileView()
    {
        this.InitializeComponent();
        this.DataContextChanged += (s, e) => Bindings.Update();
    }
}
