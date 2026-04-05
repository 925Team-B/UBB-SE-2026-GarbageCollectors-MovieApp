using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class BattleViewModelTests
    {
        private readonly Mock<IBattleService> _mockBattleService;
        private readonly Mock<IPointService> _mockPointService;
        private const int CurrentUserId = 1;

        public BattleViewModelTests()
        {
            _mockBattleService = new Mock<IBattleService>();
            _mockPointService = new Mock<IPointService>();

            _mockBattleService
                .Setup(s => s.SettleExpiredBattlesAsync())
                .Returns(Task.CompletedTask);

            _mockBattleService
                .Setup(s => s.GetCurrentBattleForUser(It.IsAny<int>()))
                .Returns(Task.FromResult<Battle?>(null));

            _mockBattleService
                .Setup(s => s.GetBet(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<Bet?>(null));

            _mockPointService
                .Setup(s => s.GetUserStats(It.IsAny<int>()))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 0 });
        }

        private BattleViewModel CreateViewModel() =>
            new(_mockBattleService.Object, _mockPointService.Object, CurrentUserId);



        [Fact]
        public async Task LoadBattleAsync_WhenCalled_SettlesExpiredBattlesFirst()
        {
            BattleViewModel vm = CreateViewModel();

            await vm.LoadBattleAsync(settleExpired: true);

            _mockBattleService.Verify(s => s.SettleExpiredBattlesAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenSettleExpiredFalse_DoesNotCallSettleExpired()
        {
            BattleViewModel vm = CreateViewModel();

            await vm.LoadBattleAsync(settleExpired: false);

            _mockBattleService.Verify(s => s.SettleExpiredBattlesAsync(), Times.Never);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenCalled_LoadsUserPoints()
        {
            _mockPointService
                .Setup(s => s.GetUserStats(CurrentUserId))
                .ReturnsAsync(new UserStats { TotalPoints = 250, WeeklyScore = 10 });

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.Equal(250, vm.TotalPoints);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenNoBattleExists_SetsHasBattleFalse()
        {
            _mockBattleService
                .Setup(s => s.GetCurrentBattleForUser(CurrentUserId))
                .Returns(Task.FromResult<Battle?>(null));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.False(vm.HasBattle);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenActiveBattleExists_SetsHasBattleTrueAndIsBattleActiveTrue()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10, Title = "Inception" },
                SecondMovie = new Movie { MovieId = 11, Title = "Interstellar" }
            };
            _mockBattleService
                .Setup(s => s.GetCurrentBattleForUser(CurrentUserId))
                .ReturnsAsync(battle);
            _mockBattleService
                .Setup(s => s.GetBet(CurrentUserId, battle.BattleId))
                .Returns(Task.FromResult<Bet?>(null));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.True(vm.HasBattle);
            Assert.True(vm.IsBattleActive);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenActiveBattle_PopulatesBetMovieOptions()
        {
            Movie movie1 = new Movie { MovieId = 10, Title = "Inception" };
            Movie movie2 = new Movie { MovieId = 11, Title = "Interstellar" };
            Battle battle = new Battle { BattleId = 1, Status = "Active", FirstMovie = movie1, SecondMovie = movie2 };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.Equal(2, vm.BetMovieOptions.Count);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenUserHasExistingBet_SetsHasBetTrue()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };
            Bet existingBet = new Bet { Amount = 50 };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync(existingBet);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.True(vm.HasBet);
            Assert.Equal(existingBet, vm.UserBet);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenBattleFinished_DeterminesWinnerAndSetsWinnerMovieName()
        {
            Movie movie1 = new Movie { MovieId = 10, Title = "Inception" };
            Movie movie2 = new Movie { MovieId = 11, Title = "Interstellar" };
            Battle battle = new Battle { BattleId = 1, Status = "Finished", FirstMovie = movie1, SecondMovie = movie2 };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.DetermineWinner(battle.BattleId)).ReturnsAsync(10);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.Equal("Inception", vm.WinnerMovieName);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenDetermineWinnerThrows_SetsWinnerMovieNameToUnknown()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Finished",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.DetermineWinner(battle.BattleId)).ThrowsAsync(new Exception("error"));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.Equal("Unknown", vm.WinnerMovieName);
        }

        [Fact]
        public async Task LoadBattleAsync_WhenCalled_ClearsStatusMessageAndHidesBetForm()
        {
            BattleViewModel vm = CreateViewModel();
            vm.StatusMessage = "old message";
            vm.ShowBetForm = true;

            await vm.LoadBattleAsync(settleExpired: false);

            Assert.Equal(string.Empty, vm.StatusMessage);
            Assert.False(vm.ShowBetForm);
        }

 

        [Fact]
        public void ShowBetFormCommand_WhenExecuted_SetsShowBetFormTrue()
        {
            BattleViewModel vm = CreateViewModel();

            vm.ShowBetFormCommand.Execute(null);

            Assert.True(vm.ShowBetForm);
        }



        [Fact]
        public async Task PlaceBetCommand_WhenNoBattleLoaded_SetsValidationStatusMessage()
        {
            BattleViewModel vm = CreateViewModel();
            // ActiveBattle is null by default
            vm.PlaceBetCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("Please select a movie and enter a valid bet amount.", vm.StatusMessage);
        }

        [Fact]
        public async Task PlaceBetCommand_WhenValidBet_CallsPlaceBetService()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).Returns(Task.FromResult<Bet?>(null));
            _mockBattleService.Setup(s => s.PlaceBet(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(new Bet()));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            vm.SelectedBetMovieId = 10;
            vm.BetAmount = 50;
            vm.PlaceBetCommand.Execute(null);
            await Task.Delay(100);

            _mockBattleService.Verify(s => s.PlaceBet(CurrentUserId, battle.BattleId, 10, 50), Times.Once);
        }

        [Fact]
        public async Task PlaceBetCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.PlaceBet(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Already bet"));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);
            vm.SelectedBetMovieId = 10;
            vm.BetAmount = 50;
            vm.PlaceBetCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Already bet", vm.StatusMessage);
        }


        [Fact]
        public async Task ForceSettleCommand_WhenNoActiveBattle_SetsStatusMessage()
        {
            BattleViewModel vm = CreateViewModel();

            vm.ForceSettleCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("No active battle to settle.", vm.StatusMessage);
        }

        [Fact]
        public async Task ForceSettleCommand_WhenActiveBattle_CallsForceSettleAndSetsSuccessMessage()
        {
            Battle battle = new Battle
            {
                BattleId = 5,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).Returns(Task.FromResult<Bet?>(null));
            _mockBattleService.Setup(s => s.ForceSettleBattleAsync(battle.BattleId)).Returns(Task.CompletedTask);
            _mockBattleService.Setup(s => s.SettleExpiredBattlesAsync()).Returns(Task.CompletedTask);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            string? capturedMessage = null;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusMessage) && !string.IsNullOrEmpty(vm.StatusMessage))
                    capturedMessage = vm.StatusMessage;
            };

            vm.ForceSettleCommand.Execute(null);
            await Task.Delay(300);

            _mockBattleService.Verify(s => s.ForceSettleBattleAsync(battle.BattleId), Times.Once);
            Assert.Equal("Battle settled! Points have been distributed.", capturedMessage);
        }

        [Fact]
        public async Task ForceSettleCommand_WhenExecuting_SetsIsProcessingThenClearsIt()
        {
            Battle battle = new Battle
            {
                BattleId = 5,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.ForceSettleBattleAsync(battle.BattleId))
                .Returns(tcs.Task);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            vm.ForceSettleCommand.Execute(null);
            await Task.Delay(30); 

            Assert.True(vm.IsProcessing);

            tcs.SetResult(true);
            await Task.Delay(100);

            Assert.False(vm.IsProcessing);
        }

        [Fact]
        public async Task ForceSettleCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessageAndClearsIsProcessing()
        {
            Battle battle = new Battle
            {
                BattleId = 5,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };

            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.ForceSettleBattleAsync(battle.BattleId))
                .ThrowsAsync(new InvalidOperationException("Cannot settle"));

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);
            vm.ForceSettleCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Cannot settle", vm.StatusMessage);
            Assert.False(vm.IsProcessing);
        }

  
        [Fact]
        public async Task ResetDemoCommand_WhenExecuted_CallsResetAndCreateDemoBattle()
        {
            _mockBattleService.Setup(s => s.ResetAllBattlesForDemoAsync()).Returns(Task.CompletedTask);
            _mockBattleService.Setup(s => s.CreateDemoBattleAsync()).Returns(Task.FromResult(new Battle()));

            BattleViewModel vm = CreateViewModel();

            string? capturedMessage = null;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusMessage) && !string.IsNullOrEmpty(vm.StatusMessage))
                    capturedMessage = vm.StatusMessage;
            };

            vm.ResetDemoCommand.Execute(null);
            await Task.Delay(300);

            _mockBattleService.Verify(s => s.ResetAllBattlesForDemoAsync(), Times.Once);
            _mockBattleService.Verify(s => s.CreateDemoBattleAsync(), Times.Once);
            Assert.Equal("Demo reset! A new battle has been created — place your bet!", capturedMessage);
        }

        [Fact]
        public async Task ResetDemoCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessageAndClearsIsProcessing()
        {
            _mockBattleService.Setup(s => s.ResetAllBattlesForDemoAsync())
                .ThrowsAsync(new InvalidOperationException("Reset failed"));

            BattleViewModel vm = CreateViewModel();
            vm.ResetDemoCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Reset failed", vm.StatusMessage);
            Assert.False(vm.IsProcessing);
        }



        [Fact]
        public async Task CanBet_WhenBattleActiveAndNoExistingBet_ReturnsTrue()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };
            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.True(vm.CanBet);
        }

        [Fact]
        public async Task CanBet_WhenUserAlreadyHasBet_ReturnsFalse()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Active",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };
            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync(new Bet());

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.False(vm.CanBet);
        }

        [Fact]
        public async Task IsBattleFinished_WhenBattleStatusIsNotActive_ReturnsTrue()
        {
            Battle battle = new Battle
            {
                BattleId = 1,
                Status = "Finished",
                FirstMovie = new Movie { MovieId = 10 },
                SecondMovie = new Movie { MovieId = 11 }
            };
            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(CurrentUserId)).ReturnsAsync(battle);
            _mockBattleService.Setup(s => s.GetBet(CurrentUserId, battle.BattleId)).ReturnsAsync((Bet?)null);
            _mockBattleService.Setup(s => s.DetermineWinner(battle.BattleId)).ThrowsAsync(new Exception());

            BattleViewModel vm = CreateViewModel();
            await vm.LoadBattleAsync(settleExpired: false);

            Assert.True(vm.IsBattleFinished);
        }
    }
}