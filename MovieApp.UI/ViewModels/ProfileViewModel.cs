#nullable enable
using System.Collections.ObjectModel;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the user profile view showing points and all badges (locked + unlocked).
/// </summary>
public class ProfileViewModel : ViewModelBase
{
    private readonly IPointService pointService;
    private readonly IBadgeService badgeService;
    private readonly int currentUserId;

    private int totalPoints;
    private int weeklyScore;

    /// <summary>
    /// Initializes a new instance of <see cref="ProfileViewModel"/>.
    /// </summary>
    public ProfileViewModel(IPointService pointService, IBadgeService badgeService, int currentUserId = 1)
    {
        this.pointService = pointService;
        this.badgeService = badgeService;
        this.currentUserId = currentUserId;

        AllBadges = new ObservableCollection<BadgeDisplayItem>();
        LoadProfileCommand = new AsyncRelayCommand(async _ => await LoadProfileAsync());
    }

    /// <summary>Gets or sets the user's total points.</summary>
    public int TotalPoints
    {
        get => totalPoints;
        set => SetProperty(ref totalPoints, value);
    }

    /// <summary>Gets or sets the user's weekly score.</summary>
    public int WeeklyScore
    {
        get => weeklyScore;
        set => SetProperty(ref weeklyScore, value);
    }

    /// <summary>Gets all badges (locked and unlocked) for display.</summary>
    public ObservableCollection<BadgeDisplayItem> AllBadges { get; }

    /// <summary>Gets the command to load profile data.</summary>
    public ICommand LoadProfileCommand { get; }

    /// <summary>
    /// Loads the user's points and badge states.
    /// Runs <see cref="IBadgeService.CheckAndAwardBadges"/> first so newly earned badges
    /// are reflected immediately when the user opens their profile.
    /// </summary>
    public async Task LoadProfileAsync()
    {
        // Ensure any newly earned badges are persisted before we read them
        await badgeService.CheckAndAwardBadges(currentUserId);

        var stats = await pointService.GetUserStats(currentUserId);
        TotalPoints = stats.TotalPoints;
        WeeklyScore = stats.WeeklyScore;

        var allBadges   = await badgeService.GetAllBadges();
        var userBadges  = await badgeService.GetUserBadges(currentUserId);
        var earnedIds   = new HashSet<int>(userBadges.Select(b => b.BadgeId));

        AllBadges.Clear();
        foreach (var badge in allBadges)
            AllBadges.Add(new BadgeDisplayItem(badge, isUnlocked: earnedIds.Contains(badge.BadgeId)));
    }
}
