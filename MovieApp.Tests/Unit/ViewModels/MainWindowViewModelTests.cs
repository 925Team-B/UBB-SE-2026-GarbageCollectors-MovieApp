using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.ViewModels
{

    public class MainWindowViewModelTests
    {
        private readonly Mock<ICatalogService> _mockCatalogService;
        private readonly Mock<IBattleService> _mockBattleService;
        private readonly Mock<IPointService> _mockPointService;
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<IBadgeService> _mockBadgeService;


        private readonly CatalogViewModel _catalogVm;
        private readonly MovieDetailViewModel _movieDetailVm;
        private readonly BattleViewModel _battleVm;
        private readonly ForumViewModel _forumVm;
        private readonly ProfileViewModel _profileVm;

        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _mockCatalogService = new Mock<ICatalogService>();
            _mockBattleService = new Mock<IBattleService>();
            _mockPointService = new Mock<IPointService>();
            _mockCommentService = new Mock<ICommentService>();
            _mockReviewService = new Mock<IReviewService>();
            _mockBadgeService = new Mock<IBadgeService>();


            _mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>());
            _mockCatalogService.Setup(s => s.SearchMovies(It.IsAny<string>())).ReturnsAsync(new List<Movie>());

            _mockBattleService.Setup(s => s.SettleExpiredBattlesAsync()).Returns(Task.CompletedTask);
            _mockBattleService.Setup(s => s.GetCurrentBattleForUser(It.IsAny<int>())).ReturnsAsync((Battle?)null);

            _mockPointService.Setup(s => s.GetUserStats(It.IsAny<int>()))
                .ReturnsAsync(new UserStats { TotalPoints = 0, WeeklyScore = 0 });

            _mockCommentService.Setup(s => s.GetCommentsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Comment>());

            _mockReviewService.Setup(s => s.GetReviewsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Review>());
            _mockReviewService.Setup(s => s.GetAverageRating(It.IsAny<int>()))
                .ReturnsAsync(0.0);

            _mockBadgeService.Setup(s => s.CheckAndAwardBadges(It.IsAny<int>())).Returns(Task.CompletedTask);
            _mockBadgeService.Setup(s => s.GetAllBadges()).ReturnsAsync(new List<Badge>());
            _mockBadgeService.Setup(s => s.GetUserBadges(It.IsAny<int>())).ReturnsAsync(new List<Badge>());


            _catalogVm = new CatalogViewModel(_mockCatalogService.Object);
            _movieDetailVm = new MovieDetailViewModel(
                _mockReviewService.Object,
                _mockCommentService.Object,
                new ExternalReviewService(new List<IExternalReviewProvider>()),
                currentUserId: 1);
            _battleVm = new BattleViewModel(_mockBattleService.Object, _mockPointService.Object, 1);
            _forumVm = new ForumViewModel(_mockCommentService.Object, _mockCatalogService.Object, 1);
            _profileVm = new ProfileViewModel(_mockPointService.Object, _mockBadgeService.Object, 1);

            _viewModel = new MainWindowViewModel(
                _catalogVm,
                _movieDetailVm,
                _battleVm,
                _forumVm,
                _profileVm);
        }



        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetAllMoviesForCatalog()
        {
            _viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            _mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsSettleExpiredBattlesForBattle()
        {
            _viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            _mockBattleService.Verify(s => s.SettleExpiredBattlesAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetAllMoviesForForum()
        {
            _viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            _mockCatalogService.Verify(s => s.GetAllMovies(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenExecuted_CallsGetUserStatsForProfile()
        {
            _viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            _mockPointService.Verify(s => s.GetUserStats(It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadDataCommand_WhenOneCatalogServiceThrows_OtherServicesStillCalled()
        {
            _mockCatalogService
                .Setup(s => s.GetAllMovies())
                .ThrowsAsync(new Exception("catalog error"));

            _viewModel.LoadDataCommand.Execute(null);
            await Task.Delay(200);

            _mockPointService.Verify(s => s.GetUserStats(It.IsAny<int>()), Times.AtLeastOnce);
        }


        [Fact]
        public void SelectedTabIndex_WhenSet_UpdatesProperty()
        {
            _viewModel.SelectedTabIndex = 3;

            Assert.Equal(3, _viewModel.SelectedTabIndex);
        }

        [Fact]
        public void SelectedTabIndex_WhenSet_RaisesPropertyChangedEvent()
        {
            bool raised = false;
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.SelectedTabIndex)) raised = true;
            };

            _viewModel.SelectedTabIndex = 2;

            Assert.True(raised);
        }


        [Fact]
        public void ShowMovieDetail_WhenSet_UpdatesProperty()
        {
            _viewModel.ShowMovieDetail = true;

            Assert.True(_viewModel.ShowMovieDetail);
        }

        [Fact]
        public void ShowMovieDetail_WhenSet_RaisesPropertyChangedEvent()
        {
            bool raised = false;
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ShowMovieDetail)) raised = true;
            };

            _viewModel.ShowMovieDetail = true;

            Assert.True(raised);
        }



        [Fact]
        public void MovieSelected_WhenRaisedByCatalog_SetsShowMovieDetailTrue()
        {
            Movie movie = new Movie { MovieId = 1, Title = "Interstellar" };

            _catalogVm.SelectMovieCommand.Execute(movie);

            Assert.True(_viewModel.ShowMovieDetail);
        }

        [Fact]
        public async Task MovieSelected_WhenRaisedByCatalog_TriggersLoadMovieOnMovieDetailVm()
        {
            Movie movie = new Movie { MovieId = 42, Title = "Interstellar" };

            _mockReviewService.Setup(s => s.GetReviewsForMovie(42)).ReturnsAsync(new List<Review>());
            _mockReviewService.Setup(s => s.GetAverageRating(42)).ReturnsAsync(8.5);
            _mockCommentService.Setup(s => s.GetCommentsForMovie(42)).ReturnsAsync(new List<Comment>());

            _catalogVm.SelectMovieCommand.Execute(movie);
            await Task.Delay(100);

            Assert.Equal(movie, _movieDetailVm.Movie);
        }



        [Fact]
        public void NavigateBack_WhenBackCommandExecutedOnDetailVm_SetsShowMovieDetailFalse()
        {
            _viewModel.ShowMovieDetail = true;

            _movieDetailVm.BackCommand.Execute(null);

            Assert.False(_viewModel.ShowMovieDetail);
        }

        [Fact]
        public async Task NavigateBack_WhenBackCommandExecutedOnDetailVm_RefreshesCatalog()
        {
            _mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>());
            int callsBefore = 0;
            _mockCatalogService
                .Setup(s => s.GetAllMovies())
                .Callback(() => callsBefore++)
                .ReturnsAsync(new List<Movie>());

            _movieDetailVm.BackCommand.Execute(null);
            await Task.Delay(100);

            Assert.True(callsBefore >= 1);
        }



        [Fact]
        public void CatalogViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(_catalogVm, _viewModel.CatalogViewModel);
        }

        [Fact]
        public void MovieDetailViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(_movieDetailVm, _viewModel.MovieDetailViewModel);
        }

        [Fact]
        public void BattleViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(_battleVm, _viewModel.BattleViewModel);
        }

        [Fact]
        public void ForumViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(_forumVm, _viewModel.ForumViewModel);
        }

        [Fact]
        public void ProfileViewModel_Accessor_ReturnsSameInstancePassedToConstructor()
        {
            Assert.Same(_profileVm, _viewModel.ProfileViewModel);
        }
    }
}