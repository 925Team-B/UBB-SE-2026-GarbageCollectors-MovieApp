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
    private readonly IPointService _pointService;
    private readonly IBadgeService _badgeService;
    private readonly int _currentUserId;

    private int _totalPoints;
    private int _weeklyScore;

    /// <summary>
    /// Initializes a new instance of <see cref="ProfileViewModel"/>.
    /// </summary>
    public ProfileViewModel(IPointService pointService, IBadgeService badgeService, int currentUserId = 1)
    {
        _pointService = pointService;
        _badgeService = badgeService;
        _currentUserId = currentUserId;

        AllBadges = new ObservableCollection<BadgeDisplayItem>();
        LoadProfileCommand = new AsyncRelayCommand(async _ => await LoadProfileAsync());
    }

    /// <summary>Gets or sets the user's total points.</summary>
    public int TotalPoints
    {
        get => _totalPoints;
        set => SetProperty(ref _totalPoints, value);
    }

    /// <summary>Gets or sets the user's weekly score.</summary>
    public int WeeklyScore
    {
        get => _weeklyScore;
        set => SetProperty(ref _weeklyScore, value);
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
        await _badgeService.CheckAndAwardBadges(_currentUserId);

        var stats = await _pointService.GetUserStats(_currentUserId);
        TotalPoints = stats.TotalPoints;
        WeeklyScore = stats.WeeklyScore;

        var allBadges   = await _badgeService.GetAllBadges();
        var userBadges  = await _badgeService.GetUserBadges(_currentUserId);
        var earnedIds   = new HashSet<int>(userBadges.Select(b => b.BadgeId));

        AllBadges.Clear();
        foreach (var badge in allBadges)
            AllBadges.Add(new BadgeDisplayItem(badge, isUnlocked: earnedIds.Contains(badge.BadgeId)));
    }
}
