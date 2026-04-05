#nullable enable
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class BattleServiceTests
{
    private readonly Mock<IBattleRepository> _battleRepoMock;
    private readonly Mock<IBetRepository> _betRepoMock;
    private readonly Mock<IMovieRepository> _movieRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPointService> _pointServiceMock;
    private readonly BattleService _sut;

    public BattleServiceTests()
    {
        _battleRepoMock = new Mock<IBattleRepository>();
        _betRepoMock = new Mock<IBetRepository>();
        _movieRepoMock = new Mock<IMovieRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _pointServiceMock = new Mock<IPointService>();

        _sut = new BattleService(
            _battleRepoMock.Object,
            _betRepoMock.Object,
            _movieRepoMock.Object,
            _userRepoMock.Object,
            _pointServiceMock.Object);
    }

    // --- GetActiveBattle ---

    [Fact]
    public async Task GetActiveBattle_WhenActiveBattleExists_ReturnsBattleWithMoviesLoaded()
    {
        var movie1 = new Movie { MovieId = 1, Title = "Inception" };
        var movie2 = new Movie { MovieId = 2, Title = "Avatar" };
        var battle = new Battle
        {
            BattleId = 1,
            Status = "Active",
            FirstMovie = new Movie { MovieId = 1 },
            SecondMovie = new Movie { MovieId = 2 }
        };
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { battle });
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await _sut.GetActiveBattle();

        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Inception", result.FirstMovie!.Title);
    }

    [Fact]
    public async Task GetActiveBattle_WhenNoActiveBattle_ReturnsNull()
    {
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>
        {
            new() { BattleId = 1, Status = "Finished" }
        });

        var result = await _sut.GetActiveBattle();

        Assert.Null(result);
    }

    // --- CreateBattle ---

    [Fact]
    public async Task CreateBattle_WhenRatingDifferenceIsWithinThreshold_CreatesBattle()
    {
        var movie1 = new Movie { MovieId = 1, AverageRating = 4.0 };
        var movie2 = new Movie { MovieId = 2, AverageRating = 4.3 };
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await _sut.CreateBattle(1, 2);

        Assert.Equal("Active", result.Status);
        Assert.Equal(1, result.FirstMovie!.MovieId);
        _battleRepoMock.Verify(r => r.Insert(It.IsAny<Battle>()), Times.Once);
    }

    [Fact]
    public async Task CreateBattle_WhenRatingDifferenceExceedsThreshold_ThrowsInvalidOperationException()
    {
        var movie1 = new Movie { MovieId = 1, AverageRating = 2.0 };
        var movie2 = new Movie { MovieId = 2, AverageRating = 5.0 };
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBattle(1, 2));
    }

    [Fact]
    public async Task CreateBattle_WhenActiveBattleAlreadyExists_ThrowsInvalidOperationException()
    {
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>
        {
            new() { BattleId = 1, Status = "Active" }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBattle(1, 2));
    }

    [Fact]
    public async Task CreateBattle_WhenFirstMovieNotFound_ThrowsInvalidOperationException()
    {
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        _movieRepoMock.Setup(r => r.GetById(1)).Returns((Movie?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateBattle(1, 2));
    }

    // --- PlaceBet ---

    [Fact]
    public async Task PlaceBet_WhenValidBet_CreatesBetAndFreezesPoints()
    {
        var user = new User { UserId = 1 };
        var battle = new Battle { BattleId = 1, Status = "Active" };
        var movie = new Movie { MovieId = 2 };

        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());
        _userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        _battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        _movieRepoMock.Setup(r => r.GetById(2)).Returns(movie);
        _pointServiceMock.Setup(p => p.FreezePoints(1, 50)).Returns(Task.CompletedTask);

        var result = await _sut.PlaceBet(1, 1, 2, 50);

        Assert.Equal(50, result.Amount);
        _pointServiceMock.Verify(p => p.FreezePoints(1, 50), Times.Once);
        _betRepoMock.Verify(r => r.Insert(It.IsAny<Bet>()), Times.Once);
    }

    [Fact]
    public async Task PlaceBet_WhenAmountIsZero_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PlaceBet(1, 1, 2, 0));
    }

    [Fact]
    public async Task PlaceBet_WhenAmountIsNegative_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PlaceBet(1, 1, 2, -10));
    }

    [Fact]
    public async Task PlaceBet_WhenUserAlreadyBetOnBattle_ThrowsInvalidOperationException()
    {
        var existingBet = new Bet
        {
            User = new User { UserId = 1 },
            Battle = new Battle { BattleId = 1 },
            Movie = new Movie { MovieId = 2 },
            Amount = 20
        };
        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { existingBet });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PlaceBet(1, 1, 2, 10));
    }

    // --- GetBet ---

    [Fact]
    public async Task GetBet_WhenBetExists_ReturnsBet()
    {
        var bet = new Bet
        {
            User = new User { UserId = 1 },
            Battle = new Battle { BattleId = 1 },
            Movie = new Movie { MovieId = 2 },
            Amount = 30
        };
        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { bet });

        var result = await _sut.GetBet(1, 1);

        Assert.NotNull(result);
        Assert.Equal(30, result.Amount);
    }

    [Fact]
    public async Task GetBet_WhenBetDoesNotExist_ReturnsNull()
    {
        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());

        var result = await _sut.GetBet(1, 1);

        Assert.Null(result);
    }

    // --- DetermineWinner ---

    [Fact]
    public async Task DetermineWinner_WhenFirstMovieImprovedMore_ReturnsFirstMovieId()
    {
        var battle = new Battle
        {
            BattleId = 1,
            InitialRatingFirstMovie = 3.0,
            InitialRatingSecondMovie = 3.0,
            FirstMovie = new Movie { MovieId = 10 },
            SecondMovie = new Movie { MovieId = 20 }
        };
        var movie1 = new Movie { MovieId = 10, AverageRating = 4.5 }; // improved by 1.5
        var movie2 = new Movie { MovieId = 20, AverageRating = 3.5 }; // improved by 0.5

        _battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        _movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);

        var winner = await _sut.DetermineWinner(1);

        Assert.Equal(10, winner);
    }

    [Fact]
    public async Task DetermineWinner_WhenSecondMovieImprovedMore_ReturnsSecondMovieId()
    {
        var battle = new Battle
        {
            BattleId = 1,
            InitialRatingFirstMovie = 3.0,
            InitialRatingSecondMovie = 3.0,
            FirstMovie = new Movie { MovieId = 10 },
            SecondMovie = new Movie { MovieId = 20 }
        };
        var movie1 = new Movie { MovieId = 10, AverageRating = 3.2 }; // improved by 0.2
        var movie2 = new Movie { MovieId = 20, AverageRating = 4.5 }; // improved by 1.5

        _battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        _movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);

        var winner = await _sut.DetermineWinner(1);

        Assert.Equal(20, winner);
    }

    [Fact]
    public async Task DetermineWinner_WhenBattleNotFound_ThrowsInvalidOperationException()
    {
        _battleRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Battle?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DetermineWinner(999));
    }

    // --- DistributePayouts ---

    [Fact]
    public async Task DistributePayouts_WhenWinnersExist_RefundsDoubleAmountToWinners()
    {
        var battle = new Battle
        {
            BattleId = 1,
            InitialRatingFirstMovie = 3.0,
            InitialRatingSecondMovie = 3.0,
            FirstMovie = new Movie { MovieId = 10 },
            SecondMovie = new Movie { MovieId = 20 },
            Status = "Active"
        };
        var winnerMovie = new Movie { MovieId = 10, AverageRating = 4.5 };
        var loserMovie = new Movie { MovieId = 20, AverageRating = 3.2 };

        var winnerBet = new Bet { User = new User { UserId = 1 }, Battle = new Battle { BattleId = 1 }, Movie = new Movie { MovieId = 10 }, Amount = 50 };
        var loserBet = new Bet { User = new User { UserId = 2 }, Battle = new Battle { BattleId = 1 }, Movie = new Movie { MovieId = 20 }, Amount = 30 };

        _battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        _movieRepoMock.Setup(r => r.GetById(10)).Returns(winnerMovie);
        _movieRepoMock.Setup(r => r.GetById(20)).Returns(loserMovie);
        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { winnerBet, loserBet });
        _pointServiceMock.Setup(p => p.RefundPoints(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        await _sut.DistributePayouts(1);

        _pointServiceMock.Verify(p => p.RefundPoints(1, 100), Times.Once); // winner gets 50*2
        _pointServiceMock.Verify(p => p.RefundPoints(2, It.IsAny<int>()), Times.Never); // loser gets nothing
        _battleRepoMock.Verify(r => r.Update(It.Is<Battle>(b => b.Status == "Finished")), Times.Once);
    }

    // --- GetCurrentBattleForUser ---

    [Fact]
    public async Task GetCurrentBattleForUser_WhenActiveBattleExists_ReturnsActiveBattle()
    {
        var movie1 = new Movie { MovieId = 1 };
        var movie2 = new Movie { MovieId = 2 };
        var activeBattle = new Battle { BattleId = 1, Status = "Active", FirstMovie = movie1, SecondMovie = movie2 };
        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { activeBattle });
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await _sut.GetCurrentBattleForUser(1);

        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task GetCurrentBattleForUser_WhenNoActiveBattle_ReturnsMostRecentFinishedBattle()
    {
        var older = new Battle { BattleId = 1, Status = "Finished", EndDate = DateTime.UtcNow.AddDays(-14), FirstMovie = new Movie { MovieId = 1 }, SecondMovie = new Movie { MovieId = 2 } };
        var newer = new Battle { BattleId = 2, Status = "Finished", EndDate = DateTime.UtcNow.AddDays(-7), FirstMovie = new Movie { MovieId = 1 }, SecondMovie = new Movie { MovieId = 2 } };

        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { older, newer });
        _movieRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns(new Movie { MovieId = 1 });

        var result = await _sut.GetCurrentBattleForUser(1);

        Assert.NotNull(result);
        Assert.Equal(2, result.BattleId);
    }

    // --- SettleExpiredBattlesAsync ---

    [Fact]
    public async Task SettleExpiredBattlesAsync_WhenActiveBattleIsExpired_DistributesPayoutsAndMarksFinished()
    {
        var expiredBattle = new Battle
        {
            BattleId = 1,
            Status = "Active",
            EndDate = DateTime.UtcNow.AddDays(-1),
            InitialRatingFirstMovie = 3.0,
            InitialRatingSecondMovie = 3.0,
            FirstMovie = new Movie { MovieId = 10 },
            SecondMovie = new Movie { MovieId = 20 }
        };
        var movie1 = new Movie { MovieId = 10, AverageRating = 4.0 };
        var movie2 = new Movie { MovieId = 20, AverageRating = 3.5 };

        _battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { expiredBattle });
        _battleRepoMock.Setup(r => r.GetById(1)).Returns(expiredBattle);
        _movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        _movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);
        _betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());
        _pointServiceMock.Setup(p => p.RefundPoints(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        await _sut.SettleExpiredBattlesAsync();

        _battleRepoMock.Verify(r => r.Update(It.Is<Battle>(b => b.Status == "Finished")), Times.Once);
    }
}
