#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for battle management and betting operations.
/// </summary>
public class BattleService : IBattleService
{
    private readonly IBattleRepository battleRepository;
    private readonly IBetRepository betRepository;
    private readonly IMovieRepository movieRepository;
    private readonly IUserRepository userRepository;
    private readonly IPointService pointService;

    /// <summary>
    /// Initializes a new instance of <see cref="BattleService"/>.
    /// </summary>
    /// <param name="battleRepository">The battle repository.</param>
    /// <param name="betRepository">The bet repository.</param>
    /// <param name="movieRepository">The movie repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="pointService">The point service for bet handling.</param>
    public BattleService(
        IBattleRepository battleRepository,
        IBetRepository betRepository,
        IMovieRepository movieRepository,
        IUserRepository userRepository,
        IPointService pointService)
    {
        this.battleRepository = battleRepository;
        this.betRepository = betRepository;
        this.movieRepository = movieRepository;
        this.userRepository = userRepository;
        this.pointService = pointService;
    }

    /// <summary>
    /// Gets the currently active battle, if any.
    /// </summary>
    /// <returns>The active battle or null.</returns>
    public async Task<Battle?> GetActiveBattle()
    {
        var battle = battleRepository.GetAll()
            .FirstOrDefault(b => b.Status == "Active");

        if (battle != null)
        {
            if (battle.FirstMovie != null)
            {
                battle.FirstMovie = movieRepository.GetById(battle.FirstMovie.MovieId) ?? battle.FirstMovie;
            }

            if (battle.SecondMovie != null)
            {
                battle.SecondMovie = movieRepository.GetById(battle.SecondMovie.MovieId) ?? battle.SecondMovie;
            }
        }

        return await Task.FromResult(battle);
    }

    /// <summary>
    /// Creates a new battle between two movies.
    /// Validates rating difference and ensures no other active battle exists.
    /// </summary>
    /// <param name="firstMovieId">The first movie's ID.</param>
    /// <param name="secondMovieId">The second movie's ID.</param>
    /// <returns>The created battle.</returns>
    /// <exception cref="InvalidOperationException">Thrown on validation failure.</exception>
    public async Task<Battle> CreateBattle(int firstMovieId, int secondMovieId)
    {
        // Check no active battle exists
        var activeBattle = battleRepository.GetAll()
            .Any(b => b.Status == "Active");
        if (activeBattle)
        {
            throw new InvalidOperationException("An active battle already exists.");
        }

        var firstMovie = movieRepository.GetById(firstMovieId)
            ?? throw new InvalidOperationException("First movie not found.");
        var secondMovie = movieRepository.GetById(secondMovieId)
            ?? throw new InvalidOperationException("Second movie not found.");

        // Validate rating difference
        if (Math.Abs(firstMovie.AverageRating - secondMovie.AverageRating) > 0.5)
        {
            throw new InvalidOperationException(
                "Rating difference between movies must be 0.5 or less.");
        }

        // Calculate start (Monday) and end (Sunday)
        var today = DateTime.UtcNow.Date;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0)
        {
            daysUntilMonday = 0; // If today is Monday, start today
        }

        var startDate = today.AddDays(daysUntilMonday);
        var endDate = startDate.AddDays(6);

        var battle = new Battle
        {
            FirstMovie = firstMovie,
            SecondMovie = secondMovie,
            InitialRatingFirstMovie = firstMovie.AverageRating,
            InitialRatingSecondMovie = secondMovie.AverageRating,
            StartDate = startDate,
            EndDate = endDate,
            Status = "Active"
        };

        battleRepository.Insert(battle);

        return battle;
    }

    /// <summary>
    /// Places a bet on a battle.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="battleId">The battle's ID.</param>
    /// <param name="movieId">The movie to bet on.</param>
    /// <param name="amount">Points to bet (must be > 0).</param>
    /// <returns>The created bet.</returns>
    /// <exception cref="InvalidOperationException">Thrown if user already bet or invalid amount.</exception>
    public async Task<Bet> PlaceBet(int userId, int battleId, int movieId, int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Bet amount must be greater than 0.");
        }

        // Check if user already bet on this battle
        var existingBet = betRepository.GetAll()
            .Any(b => b.User?.UserId == userId && b.Battle?.BattleId == battleId);
        if (existingBet)
        {
            throw new InvalidOperationException("User has already placed a bet on this battle.");
        }

        var user = userRepository.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");
        var battle = battleRepository.GetById(battleId)
            ?? throw new InvalidOperationException("Battle not found.");
        var movie = movieRepository.GetById(movieId)
            ?? throw new InvalidOperationException("Movie not found.");

        // Freeze the points
        await pointService.FreezePoints(userId, amount);

        var bet = new Bet
        {
            User = user,
            Battle = battle,
            Movie = movie,
            Amount = amount
        };

        betRepository.Insert(bet);

        return bet;
    }

    /// <summary>
    /// Gets a user's bet for a specific battle.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="battleId">The battle's ID.</param>
    /// <returns>The user's bet or null.</returns>
    public async Task<Bet?> GetBet(int userId, int battleId)
    {
        var bet = betRepository.GetAll()
            .FirstOrDefault(b => b.User?.UserId == userId && b.Battle?.BattleId == battleId);

        return await Task.FromResult(bet);
    }

    /// <summary>
    /// Determines the winner of a battle based on rating improvement.
    /// </summary>
    /// <param name="battleId">The battle identifier.</param>
    /// <returns>The winning movie's ID.</returns>
    public async Task<int> DetermineWinner(int battleId)
    {
        var battle = battleRepository.GetById(battleId)
            ?? throw new InvalidOperationException("Battle not found.");

        if (battle.FirstMovie != null)
        {
            battle.FirstMovie = movieRepository.GetById(battle.FirstMovie.MovieId) ?? battle.FirstMovie;
        }

        if (battle.SecondMovie != null)
        {
            battle.SecondMovie = movieRepository.GetById(battle.SecondMovie.MovieId) ?? battle.SecondMovie;
        }

        double firstImprovement = (battle.FirstMovie?.AverageRating ?? 0) - battle.InitialRatingFirstMovie;
        double secondImprovement = (battle.SecondMovie?.AverageRating ?? 0) - battle.InitialRatingSecondMovie;

        return firstImprovement >= secondImprovement
            ? (battle.FirstMovie?.MovieId ?? 0)
            : (battle.SecondMovie?.MovieId ?? 0);
    }

    /// <summary>
    /// Gets the active battle, or the most recent battle the user has bet on if no active battle exists.
    /// </summary>
    public async Task<Battle?> GetCurrentBattleForUser(int userId)
    {
        var active = await GetActiveBattle();
        if (active != null)
        {
            return active;
        }

        // No active battle — show the most recent battle (so users can always see the last matchup)
        var recentBattle = battleRepository.GetAll()
            .OrderByDescending(b => b.EndDate)
            .FirstOrDefault();

        if (recentBattle != null)
        {
            if (recentBattle.FirstMovie != null)
            {
                recentBattle.FirstMovie = movieRepository.GetById(recentBattle.FirstMovie.MovieId) ?? recentBattle.FirstMovie;
            }

            if (recentBattle.SecondMovie != null)
            {
                recentBattle.SecondMovie = movieRepository.GetById(recentBattle.SecondMovie.MovieId) ?? recentBattle.SecondMovie;
            }
        }

        return recentBattle;
    }

    /// <summary>
    /// Settles any active battles whose end date has already passed.
    /// Called on startup so points are always returned after a week ends.
    /// </summary>
    public async Task SettleExpiredBattlesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var expired = battleRepository.GetAll()
            .Where(b => b.Status == "Active" && b.EndDate < today)
            .ToList();

        foreach (var battle in expired)
        {
            await DistributePayouts(battle.BattleId);
        }
    }

    /// <summary>
    /// Distributes payouts to winning bettors (Amount * 2).
    /// </summary>
    /// <param name="battleId">The battle identifier.</param>
    public async Task DistributePayouts(int battleId)
    {
        int winningMovieId = await DetermineWinner(battleId);

        var bets = betRepository.GetAll()
            .Where(b => b.Battle?.BattleId == battleId)
            .ToList();

        foreach (var bet in bets)
        {
            if (bet.Movie?.MovieId == winningMovieId)
            {
                // Winner gets Amount * 2
                await pointService.RefundPoints(bet.User?.UserId ?? 0, bet.Amount * 2);
            }
            // Losers lose their frozen points (already deducted)
        }

        var battle = battleRepository.GetById(battleId);
        if (battle != null)
        {
            battle.Status = "Finished";
            battleRepository.Update(battle);
        }
    }

    public Task ForceSettleBattleAsync(int battleId)
    {
        throw new NotImplementedException();
    }

    public Task ResetAllBattlesForDemoAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Battle> CreateDemoBattleAsync()
    {
        throw new NotImplementedException();
    }
}
