#nullable enable
using System.Collections.ObjectModel;
using System.Windows.Input;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;

namespace MovieApp.UI.ViewModels;

/// <summary>
/// ViewModel for the battle arena view showing active battles, betting, and demo controls.
/// </summary>
public class BattleViewModel : ViewModelBase
{
    private readonly IBattleService _battleService;
    private readonly IPointService _pointService;
    private readonly int _currentUserId;

    private Battle? _activeBattle;
    private bool _hasBattle;
    private bool _isBattleActive;
    private bool _showBetForm;
    private int _betAmount;
    private int _selectedBetMovieId;
    private int _totalPoints;
    private string _statusMessage = string.Empty;
    private Bet? _userBet;
    private bool _hasBet;
    private string _winnerMovieName = string.Empty;
    private bool _isProcessing;

    /// <summary>
    /// Initializes a new instance of <see cref="BattleViewModel"/>.
    /// </summary>
    public BattleViewModel(IBattleService battleService, IPointService pointService, int currentUserId = 1)
    {
        _battleService = battleService;
        _pointService = pointService;
        _currentUserId = currentUserId;

        LoadBattleCommand     = new AsyncRelayCommand(async _ => await LoadBattleAsync());
        ShowBetFormCommand    = new RelayCommand(_ => ShowBetForm = true);
        PlaceBetCommand       = new AsyncRelayCommand(async _ => await PlaceBetAsync());
        ForceSettleCommand    = new AsyncRelayCommand(async _ => await ForceSettleAsync());
        ResetDemoCommand      = new AsyncRelayCommand(async _ => await ResetDemoAsync());
    }

    /// <summary>Gets the available movies to bet on.</summary>
    public ObservableCollection<Movie> BetMovieOptions { get; } = new();

    /// <summary>Gets or sets the active battle.</summary>
    public Battle? ActiveBattle
    {
        get => _activeBattle;
        set => SetProperty(ref _activeBattle, value);
    }

    /// <summary>Gets or sets whether there is a battle to display.</summary>
    public bool HasBattle
    {
        get => _hasBattle;
        set
        {
            if (SetProperty(ref _hasBattle, value))
                OnPropertyChanged(nameof(IsBattleFinished));
        }
    }

    /// <summary>Gets or sets whether the current battle is still active (not finished).</summary>
    public bool IsBattleActive
    {
        get => _isBattleActive;
        set
        {
            if (SetProperty(ref _isBattleActive, value))
            {
                OnPropertyChanged(nameof(CanBet));
                OnPropertyChanged(nameof(IsBattleFinished));
            }
        }
    }

    /// <summary>Gets whether the user can place a bet (battle active, no bet yet).</summary>
    public bool CanBet => IsBattleActive && !HasBet;

    /// <summary>Gets whether a finished battle is being displayed.</summary>
    public bool IsBattleFinished => HasBattle && !IsBattleActive;

    /// <summary>Gets or sets whether to show the bet form.</summary>
    public bool ShowBetForm
    {
        get => _showBetForm;
        set => SetProperty(ref _showBetForm, value);
    }

    /// <summary>Gets or sets the bet amount.</summary>
    public int BetAmount
    {
        get => _betAmount;
        set => SetProperty(ref _betAmount, value);
    }

    /// <summary>Gets or sets the movie ID the user is betting on.</summary>
    public int SelectedBetMovieId
    {
        get => _selectedBetMovieId;
        set => SetProperty(ref _selectedBetMovieId, value);
    }

    /// <summary>Gets or sets the user's total points.</summary>
    public int TotalPoints
    {
        get => _totalPoints;
        set => SetProperty(ref _totalPoints, value);
    }

    /// <summary>Gets or sets a status message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets or sets the user's existing bet.</summary>
    public Bet? UserBet
    {
        get => _userBet;
        set => SetProperty(ref _userBet, value);
    }

    /// <summary>Gets or sets whether the user has already placed a bet.</summary>
    public bool HasBet
    {
        get => _hasBet;
        set
        {
            if (SetProperty(ref _hasBet, value))
                OnPropertyChanged(nameof(CanBet));
        }
    }

    /// <summary>Gets or sets the title of the winning movie (populated after battle ends).</summary>
    public string WinnerMovieName
    {
        get => _winnerMovieName;
        set => SetProperty(ref _winnerMovieName, value);
    }

    /// <summary>Gets or sets whether an async operation is in progress (disables demo buttons).</summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set => SetProperty(ref _isProcessing, value);
    }

    /// <summary>Gets the command to load the active battle.</summary>
    public ICommand LoadBattleCommand { get; }
    /// <summary>Gets the command to show the bet form.</summary>
    public ICommand ShowBetFormCommand { get; }
    /// <summary>Gets the command to place a bet.</summary>
    public ICommand PlaceBetCommand { get; }
    /// <summary>Gets the command to immediately settle the current active battle (demo).</summary>
    public ICommand ForceSettleCommand { get; }
    /// <summary>Gets the command to delete all battles and create a fresh one (demo).</summary>
    public ICommand ResetDemoCommand { get; }

    /// <summary>
    /// Loads the current battle and user's points.
    /// </summary>
    /// <param name="settleExpired">When true (default), expired battles are settled first.</param>
    public async Task LoadBattleAsync(bool settleExpired = true)
    {
        StatusMessage = string.Empty;
        ShowBetForm = false;

        if (settleExpired)
            await _battleService.SettleExpiredBattlesAsync();

        var stats = await _pointService.GetUserStats(_currentUserId);
        TotalPoints = stats.TotalPoints;

        ActiveBattle = await _battleService.GetCurrentBattleForUser(_currentUserId);
        HasBattle    = ActiveBattle != null;
        IsBattleActive = ActiveBattle?.Status == "Active";

        if (ActiveBattle != null)
        {
            BetMovieOptions.Clear();
            if (ActiveBattle.FirstMovie  != null) BetMovieOptions.Add(ActiveBattle.FirstMovie);
            if (ActiveBattle.SecondMovie != null) BetMovieOptions.Add(ActiveBattle.SecondMovie);

            UserBet = await _battleService.GetBet(_currentUserId, ActiveBattle.BattleId);
            HasBet  = UserBet != null;

            // If finished, show who won
            if (IsBattleFinished)
            {
                try
                {
                    int winId = await _battleService.DetermineWinner(ActiveBattle.BattleId);
                    WinnerMovieName = winId == ActiveBattle.FirstMovie?.MovieId
                        ? ActiveBattle.FirstMovie?.Title ?? "Movie 1"
                        : ActiveBattle.SecondMovie?.Title ?? "Movie 2";
                }
                catch
                {
                    WinnerMovieName = "Unknown";
                }
            }
            else
            {
                WinnerMovieName = string.Empty;
            }
        }
    }

    /// <summary>Places a bet on the active battle.</summary>
    private async Task PlaceBetAsync()
    {
        if (ActiveBattle == null || SelectedBetMovieId <= 0 || BetAmount <= 0)
        {
            StatusMessage = "Please select a movie and enter a valid bet amount.";
            return;
        }

        try
        {
            await _battleService.PlaceBet(_currentUserId, ActiveBattle.BattleId, SelectedBetMovieId, BetAmount);
            StatusMessage = $"Bet of {BetAmount} points placed successfully!";
            ShowBetForm = false;
            await LoadBattleAsync(settleExpired: false);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Immediately settles the active battle regardless of its end date.
    /// Determines the winner, distributes payouts, then reloads.
    /// </summary>
    private async Task ForceSettleAsync()
    {
        if (ActiveBattle == null || !IsBattleActive)
        {
            StatusMessage = "No active battle to settle.";
            return;
        }

        IsProcessing = true;
        try
        {
            await _battleService.ForceSettleBattleAsync(ActiveBattle.BattleId);
            StatusMessage = "Battle settled! Points have been distributed.";
            await LoadBattleAsync(settleExpired: false);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Resets the demo: deletes all battles/bets (refunding frozen points),
    /// then creates a brand-new active battle so a fresh bet can be placed.
    /// </summary>
    private async Task ResetDemoAsync()
    {
        IsProcessing = true;
        StatusMessage = string.Empty;
        try
        {
            await _battleService.ResetAllBattlesForDemoAsync();
            await _battleService.CreateDemoBattleAsync();
            StatusMessage = "Demo reset! A new battle has been created — place your bet!";
            await LoadBattleAsync(settleExpired: false);
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
