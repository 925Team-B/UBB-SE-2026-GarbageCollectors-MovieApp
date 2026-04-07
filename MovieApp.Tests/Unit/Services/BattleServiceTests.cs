#nullable enable
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class BattleServiceTests
{
    private readonly Mock<IBattleRepository> battleRepoMock;
    private readonly Mock<IBetRepository> betRepoMock;
    private readonly Mock<IMovieRepository> movieRepoMock;
    private readonly Mock<IUserRepository> userRepoMock;
    private readonly Mock<IPointService> pointServiceMock;
    private readonly BattleService sut;

    public BattleServiceTests()
    {
        battleRepoMock = new Mock<IBattleRepository>();
        betRepoMock = new Mock<IBetRepository>();
        movieRepoMock = new Mock<IMovieRepository>();
        userRepoMock = new Mock<IUserRepository>();
        pointServiceMock = new Mock<IPointService>();

        sut = new BattleService(
            battleRepoMock.Object,
            betRepoMock.Object,
            movieRepoMock.Object,
            userRepoMock.Object,
            pointServiceMock.Object);
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
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { battle });
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await sut.GetActiveBattle();

        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Inception", result.FirstMovie!.Title);
    }

    [Fact]
    public async Task GetActiveBattle_WhenNoActiveBattle_ReturnsNull()
    {
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>
        {
            new () { BattleId = 1, Status = "Finished" }
        });

        var result = await sut.GetActiveBattle();

        Assert.Null(result);
    }

    // --- CreateBattle ---
    [Fact]
    public async Task CreateBattle_WhenRatingDifferenceIsWithinThreshold_CreatesBattle()
    {
        var movie1 = new Movie { MovieId = 1, AverageRating = 4.0 };
        var movie2 = new Movie { MovieId = 2, AverageRating = 4.3 };
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await sut.CreateBattle(1, 2);

        Assert.Equal("Active", result.Status);
        Assert.Equal(1, result.FirstMovie!.MovieId);
        battleRepoMock.Verify(r => r.Insert(It.IsAny<Battle>()), Times.Once);
    }

    [Fact]
    public async Task CreateBattle_WhenRatingDifferenceExceedsThreshold_ThrowsInvalidOperationException()
    {
        var movie1 = new Movie { MovieId = 1, AverageRating = 2.0 };
        var movie2 = new Movie { MovieId = 2, AverageRating = 5.0 };
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBattle(1, 2));
    }

    [Fact]
    public async Task CreateBattle_WhenActiveBattleAlreadyExists_ThrowsInvalidOperationException()
    {
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>
        {
            new () { BattleId = 1, Status = "Active" }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBattle(1, 2));
    }

    [Fact]
    public async Task CreateBattle_WhenFirstMovieNotFound_ThrowsInvalidOperationException()
    {
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle>());
        movieRepoMock.Setup(r => r.GetById(1)).Returns((Movie?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateBattle(1, 2));
    }

    // --- PlaceBet ---
    [Fact]
    public async Task PlaceBet_WhenValidBet_CreatesBetAndFreezesPoints()
    {
        var user = new User { UserId = 1 };
        var battle = new Battle { BattleId = 1, Status = "Active" };
        var movie = new Movie { MovieId = 2 };

        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());
        userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        movieRepoMock.Setup(r => r.GetById(2)).Returns(movie);
        pointServiceMock.Setup(p => p.FreezePoints(1, 50)).Returns(Task.CompletedTask);

        var result = await sut.PlaceBet(1, 1, 2, 50);

        Assert.Equal(50, result.Amount);
        pointServiceMock.Verify(p => p.FreezePoints(1, 50), Times.Once);
        betRepoMock.Verify(r => r.Insert(It.IsAny<Bet>()), Times.Once);
    }

    [Fact]
    public async Task PlaceBet_WhenAmountIsZero_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PlaceBet(1, 1, 2, 0));
    }

    [Fact]
    public async Task PlaceBet_WhenAmountIsNegative_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PlaceBet(1, 1, 2, -10));
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
        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { existingBet });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PlaceBet(1, 1, 2, 10));
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
        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { bet });

        var result = await sut.GetBet(1, 1);

        Assert.NotNull(result);
        Assert.Equal(30, result.Amount);
    }

    [Fact]
    public async Task GetBet_WhenBetDoesNotExist_ReturnsNull()
    {
        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());

        var result = await sut.GetBet(1, 1);

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

        battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);

        var winner = await sut.DetermineWinner(1);

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

        battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);

        var winner = await sut.DetermineWinner(1);

        Assert.Equal(20, winner);
    }

    [Fact]
    public async Task DetermineWinner_WhenBattleNotFound_ThrowsInvalidOperationException()
    {
        battleRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Battle?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DetermineWinner(999));
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

        battleRepoMock.Setup(r => r.GetById(1)).Returns(battle);
        movieRepoMock.Setup(r => r.GetById(10)).Returns(winnerMovie);
        movieRepoMock.Setup(r => r.GetById(20)).Returns(loserMovie);
        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet> { winnerBet, loserBet });
        pointServiceMock.Setup(p => p.RefundPoints(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        await sut.DistributePayouts(1);

        pointServiceMock.Verify(p => p.RefundPoints(1, 100), Times.Once); // winner gets 50*2
        pointServiceMock.Verify(p => p.RefundPoints(2, It.IsAny<int>()), Times.Never); // loser gets nothing
        battleRepoMock.Verify(r => r.Update(It.Is<Battle>(b => b.Status == "Finished")), Times.Once);
    }

    // --- GetCurrentBattleForUser ---
    [Fact]
    public async Task GetCurrentBattleForUser_WhenActiveBattleExists_ReturnsActiveBattle()
    {
        var movie1 = new Movie { MovieId = 1 };
        var movie2 = new Movie { MovieId = 2 };
        var activeBattle = new Battle { BattleId = 1, Status = "Active", FirstMovie = movie1, SecondMovie = movie2 };
        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { activeBattle });
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(2)).Returns(movie2);

        var result = await sut.GetCurrentBattleForUser(1);

        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task GetCurrentBattleForUser_WhenNoActiveBattle_ReturnsMostRecentFinishedBattle()
    {
        var older = new Battle { BattleId = 1, Status = "Finished", EndDate = DateTime.UtcNow.AddDays(-14), FirstMovie = new Movie { MovieId = 1 }, SecondMovie = new Movie { MovieId = 2 } };
        var newer = new Battle { BattleId = 2, Status = "Finished", EndDate = DateTime.UtcNow.AddDays(-7), FirstMovie = new Movie { MovieId = 1 }, SecondMovie = new Movie { MovieId = 2 } };

        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { older, newer });
        movieRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns(new Movie { MovieId = 1 });

        var result = await sut.GetCurrentBattleForUser(1);

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

        battleRepoMock.Setup(r => r.GetAll()).Returns(new List<Battle> { expiredBattle });
        battleRepoMock.Setup(r => r.GetById(1)).Returns(expiredBattle);
        movieRepoMock.Setup(r => r.GetById(10)).Returns(movie1);
        movieRepoMock.Setup(r => r.GetById(20)).Returns(movie2);
        betRepoMock.Setup(r => r.GetAll()).Returns(new List<Bet>());
        pointServiceMock.Setup(p => p.RefundPoints(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        await sut.SettleExpiredBattlesAsync();

        battleRepoMock.Verify(r => r.Update(It.Is<Battle>(b => b.Status == "Finished")), Times.Once);
    }
}
