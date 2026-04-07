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
    private readonly IBattleService battleService;
    private readonly IPointService pointService;
    private readonly int currentUserId;

    private Battle? activeBattle;
    private bool hasBattle;
    private bool isBattleActive;
    private bool showBetForm;
    private int betAmount;
    private int selectedBetMovieId;
    private int totalPoints;
    private string statusMessage = string.Empty;
    private Bet? userBet;
    private bool hasBet;
    private string winnerMovieName = string.Empty;
    private bool isProcessing;

    /// <summary>
    /// Initializes a new instance of <see cref="BattleViewModel"/>.
    /// </summary>
    public BattleViewModel(IBattleService battleService, IPointService pointService, int currentUserId = 1)
    {
        this.battleService = battleService;
        this.pointService = pointService;
        this.currentUserId = currentUserId;

        this.LoadBattleCommand = new AsyncRelayCommand(async _ => await this.LoadBattleAsync());
        this.ShowBetFormCommand = new RelayCommand(_ => this.ShowBetForm = true);
        this.PlaceBetCommand = new AsyncRelayCommand(async _ => await this.PlaceBetAsync());
        this.ForceSettleCommand = new AsyncRelayCommand(async _ => await this.ForceSettleAsync());
        this.ResetDemoCommand = new AsyncRelayCommand(async _ => await this.ResetDemoAsync());
    }

    /// <summary>Gets the available movies to bet on.</summary>
    public ObservableCollection<Movie> BetMovieOptions { get; } = new ();

    /// <summary>Gets or sets the active battle.</summary>
    public Battle? ActiveBattle
    {
        get => this.activeBattle;
        set => this.SetProperty(ref this.activeBattle, value);
    }

    /// <summary>Gets or sets whether there is a battle to display.</summary>
    public bool HasBattle
    {
        get => this.hasBattle;
        set
        {
            if (this.SetProperty(ref this.hasBattle, value))
            {
                this.OnPropertyChanged(nameof(this.IsBattleFinished));
            }
        }
    }

    /// <summary>Gets or sets whether the current battle is still active (not finished).</summary>
    public bool IsBattleActive
    {
        get => isBattleActive;
        set
        {
            if (SetProperty(ref isBattleActive, value))
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
        get => showBetForm;
        set => SetProperty(ref showBetForm, value);
    }

    /// <summary>Gets or sets the bet amount.</summary>
    public int BetAmount
    {
        get => betAmount;
        set => SetProperty(ref betAmount, value);
    }

    /// <summary>Gets or sets the movie ID the user is betting on.</summary>
    public int SelectedBetMovieId
    {
        get => selectedBetMovieId;
        set => SetProperty(ref selectedBetMovieId, value);
    }

    /// <summary>Gets or sets the user's total points.</summary>
    public int TotalPoints
    {
        get => totalPoints;
        set => SetProperty(ref totalPoints, value);
    }

    /// <summary>Gets or sets a status message.</summary>
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    /// <summary>Gets or sets the user's existing bet.</summary>
    public Bet? UserBet
    {
        get => userBet;
        set => SetProperty(ref userBet, value);
    }

    /// <summary>Gets or sets whether the user has already placed a bet.</summary>
    public bool HasBet
    {
        get => hasBet;
        set
        {
            if (SetProperty(ref hasBet, value))
            {
                OnPropertyChanged(nameof(CanBet));
            }
        }
    }

    /// <summary>Gets or sets the title of the winning movie (populated after battle ends).</summary>
    public string WinnerMovieName
    {
        get => winnerMovieName;
        set => SetProperty(ref winnerMovieName, value);
    }

    /// <summary>Gets or sets whether an async operation is in progress (disables demo buttons).</summary>
    public bool IsProcessing
    {
        get => isProcessing;
        set => SetProperty(ref isProcessing, value);
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
        {
            await battleService.SettleExpiredBattlesAsync();
        }

        var stats = await pointService.GetUserStats(currentUserId);
        TotalPoints = stats.TotalPoints;

        ActiveBattle = await battleService.GetCurrentBattleForUser(currentUserId);
        HasBattle = ActiveBattle != null;
        IsBattleActive = ActiveBattle?.Status == "Active";

        if (ActiveBattle != null)
        {
            BetMovieOptions.Clear();
            if (ActiveBattle.FirstMovie != null)
            {
                this.BetMovieOptions.Add(ActiveBattle.FirstMovie);
            }

            if (ActiveBattle.SecondMovie != null)
            {
                BetMovieOptions.Add(ActiveBattle.SecondMovie);
            }

            UserBet = await battleService.GetBet(currentUserId, ActiveBattle.BattleId);
            HasBet = UserBet != null;

            // If finished, show who won
            if (IsBattleFinished)
            {
                try
                {
                    int winId = await battleService.DetermineWinner(ActiveBattle.BattleId);
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
            await battleService.PlaceBet(currentUserId, ActiveBattle.BattleId, SelectedBetMovieId, BetAmount);
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
            await battleService.ForceSettleBattleAsync(ActiveBattle.BattleId);
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
            await battleService.ResetAllBattlesForDemoAsync();
            await battleService.CreateDemoBattleAsync();
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
