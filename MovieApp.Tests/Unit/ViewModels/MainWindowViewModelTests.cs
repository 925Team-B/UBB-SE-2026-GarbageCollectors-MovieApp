using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Moq;
using Xunit;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class MainWindowViewModelTests
    {
        private readonly Mock<ICatalogService> mockCatalogService;
        private readonly Mock<IBattleService> mockBattleService;
        private readonly Mock<IPointService> mockPointService;
        private readonly Mock<ICommentService> mockCommentService;
        private readonly Mock<IReviewService> mockReviewService;
        private readonly Mock<IBadgeService> mockBadgeService;
        private readonly CatalogViewModel catalogVm;
        private readonly MovieDetailViewModel movieDetailVm;
        private readonly BattleViewModel battleVm;
        private readonly ForumViewModel forumVm;
        private readonly ProfileViewModel profileVm;

        private readonly MainWindowViewModel viewModel;

        public MainWindowViewModelTests()
        {
            mockCatalogService = new Mock<ICatalogService>();
            mockBattleService = new Mock<IBattleService>();
            mockPointService = new Mock<IPointService>();
            mockCommentService = new Mock<ICommentService>();
            mockReviewService = new Mock<IReviewService>();
            mockBadgeService = new Mock<IBadgeService>();
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>());
            mockCatalogService.Setup(s => s.SearchMovies(It.IsAny<string>())).ReturnsAsync(new List<Movie>());

            mockBattleService.Setup(s => s.SettleExpiredBattlesAsync()).Returns(Task.CompletedTask);
            mockBattleService.Setup(s => s.GetCurrentBattleForUser(It.IsAny<int>())).ReturnsAsync((Battle?)null);

            mockPointService.Setup(s => s.GetUserStats(It.IsAny<int>()))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 0 });

            mockCommentService.Setup(s => s.GetCommentsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Comment>());

            mockReviewService.Setup(s => s.GetReviewsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Review>());
            mockReviewService.Setup(s => s.GetAverageRating(It.IsAny<int>()))
                .ReturnsAsync(0.0);

            mockBadgeService.Setup(s => s.CheckAndAwardBadges(It.IsAny<int>())).Returns(Task.CompletedTask);
            mockBadgeService.Setup(s => s.GetAllBadges()).ReturnsAsync(new List<Badge>());
            mockBadgeService.Setup(s => s.GetUserBadges(It.IsAny<int>())).ReturnsAsync(new List<Badge>());
            catalogVm = new CatalogViewModel(mockCatalogService.Object);
            movieDetailVm = new MovieDetailViewModel(
                mockReviewService.Object,
                mockCommentService.Object,
                new ExternalReviewService(new List<IExternalReviewProvider>()),
                currentUserId: 1);
            battleVm = new BattleViewModel(mockBattleService.Object, mockPointService.Object, 1);
            forumVm = new ForumViewModel(mockCommentService.Object, mockCatalogService.Object, 1);
            profileVm = new ProfileViewModel(mockPointService.Object, mockBadgeService.Object, 1);

            viewModel = new MainWindowViewModel(
                catalogVm,
                movieDetailVm,
                battleVm,
                forumVm,
                profileVm);
        }
        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetAllMoviesForCatalog()
        {
            viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsSettleExpiredBattlesForBattle()
        {
            viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            mockBattleService.Verify(s => s.SettleExpiredBattlesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetAllMoviesForForum()
        {
            viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetUserStatsForProfile()
        {
            viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            mockPointService.Verify(s => s.GetUserStats(It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenOneCatalogServiceThrows_OtherServicesStillCalled()
        {
            mockCatalogService
                .Setup(s => s.GetAllMovies())
                .ThrowsAsync(new Exception("catalog error"));

            viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            mockPointService.Verify(s => s.GetUserStats(It.IsAny<int>()), Times.AtLeastOnce);
        }
        [Fact]
        public void SelectedTabIndex_WhenSet_UpdatesProperty()
        {
            viewModel.SelectedTabIndex = 3;

            Assert.Equal(3, viewModel.SelectedTabIndex);
        }

        [Fact]
        public void SelectedTabIndex_WhenSet_RaisesPropertyChangedEvent()
        {
            bool raised = false;
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(viewModel.SelectedTabIndex))
                {
                    raised = true;
                }
            };

            viewModel.SelectedTabIndex = 2;

            Assert.True(raised);
        }
        [Fact]
        public void ShowMovieDetail_WhenSet_UpdatesProperty()
        {
            viewModel.ShowMovieDetail = true;

            Assert.True(viewModel.ShowMovieDetail);
        }

        [Fact]
        public void ShowMovieDetail_WhenSet_RaisesPropertyChangedEvent()
        {
            bool raised = false;
            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(viewModel.ShowMovieDetail))
                {
                    raised = true;
                }
            };

            viewModel.ShowMovieDetail = true;

            Assert.True(raised);
        }
        [Fact]
        public void MovieSelected_WhenRaisedByCatalog_SetsShowMovieDetailTrue()
        {
            Movie movie = new Movie { MovieId = 1, Title = "Interstellar" };

            catalogVm.SelectMovieCommand.Execute(movie);

            Assert.True(viewModel.ShowMovieDetail);
        }

        [Fact]
        public async Task MovieSelected_WhenRaisedByCatalog_TriggersLoadMovieOnMovieDetailVm()
        {
            Movie movie = new Movie { MovieId = 42, Title = "Interstellar" };

            mockReviewService.Setup(s => s.GetReviewsForMovie(42)).ReturnsAsync(new List<Review>());
            mockReviewService.Setup(s => s.GetAverageRating(42)).ReturnsAsync(8.5);
            mockCommentService.Setup(s => s.GetCommentsForMovie(42)).ReturnsAsync(new List<Comment>());

            catalogVm.SelectMovieCommand.Execute(movie);
            await Task.Delay(100);

            Assert.Equal(movie, movieDetailVm.Movie);
        }
        [Fact]
        public void NavigateBack_WhenBackCommandExecutedOnDetailVm_SetsShowMovieDetailFalse()
        {
            viewModel.ShowMovieDetail = true;

            movieDetailVm.BackCommand.Execute(null);

            Assert.False(viewModel.ShowMovieDetail);
        }

        [Fact]
        public async Task NavigateBack_WhenBackCommandExecutedOnDetailVm_RefreshesCatalog()
        {
            mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>());
            int callsBefore = 0;
            mockCatalogService
                .Setup(s => s.GetAllMovies())
                .Callback(() => callsBefore++)
                .ReturnsAsync(new List<Movie>());

            movieDetailVm.BackCommand.Execute(null);
            await Task.Delay(100);

            Assert.True(callsBefore >= 1);
        }
        [Fact]
        public void CatalogViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(catalogVm, viewModel.CatalogViewModel);
        }

        [Fact]
        public void MovieDetailViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(movieDetailVm, viewModel.MovieDetailViewModel);
        }

        [Fact]
        public void BattleViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(battleVm, viewModel.BattleViewModel);
        }

        [Fact]
        public void ForumViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(forumVm, viewModel.ForumViewModel);
        }

        [Fact]
        public void ProfileViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(profileVm, viewModel.ProfileViewModel);
        }
    }
}