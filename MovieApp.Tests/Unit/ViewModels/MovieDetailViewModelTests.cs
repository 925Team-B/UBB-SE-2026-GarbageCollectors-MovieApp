using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class MovieDetailViewModelTests
    {
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly ExternalReviewService _externalReviewService;
        private const int CurrentUserId = 1;

        private readonly Movie _testMovie = new()
        {
            MovieId = 42,
            Title = "Inception",
            Year = 2010
        };

        public MovieDetailViewModelTests()
        {
            _mockReviewService = new Mock<IReviewService>();
            _mockCommentService = new Mock<ICommentService>();
            _externalReviewService = new ExternalReviewService(new List<IExternalReviewProvider>());

            _mockReviewService.Setup(s => s.GetReviewsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Review>());
            _mockReviewService.Setup(s => s.GetAverageRating(It.IsAny<int>()))
                .ReturnsAsync(0.0);
            _mockCommentService.Setup(s => s.GetCommentsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Comment>());
        }

        private MovieDetailViewModel CreateViewModel() =>
            new(_mockReviewService.Object, _mockCommentService.Object, _externalReviewService, CurrentUserId);



        [Fact]
        public async Task LoadMovieAsync_WhenCalled_SetsMovieProperty()
        {
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            Assert.Equal(_testMovie, vm.Movie);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_CallsGetReviewsForMovie()
        {
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            _mockReviewService.Verify(s => s.GetReviewsForMovie(_testMovie.MovieId), Times.Once);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_CallsGetAverageRating()
        {
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            _mockReviewService.Verify(s => s.GetAverageRating(_testMovie.MovieId), Times.Once);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_SetsAverageRating()
        {
            _mockReviewService.Setup(s => s.GetAverageRating(_testMovie.MovieId)).ReturnsAsync(8.5);
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            Assert.Equal(8.5, vm.AverageRating);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_PopulatesReviewsCollection()
        {
            _mockReviewService.Setup(s => s.GetReviewsForMovie(_testMovie.MovieId))
                .ReturnsAsync(new List<Review>
                {
                    new() { ReviewId = 1, User = new User { UserId = 99 } },
                    new() { ReviewId = 2, User = new User { UserId = 98 } }
                });
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            Assert.Equal(2, vm.Reviews.Count);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCurrentUserHasReview_SetsHasUserReviewTrue()
        {
            _mockReviewService.Setup(s => s.GetReviewsForMovie(_testMovie.MovieId))
                .ReturnsAsync(new List<Review>
                {
                    new() { ReviewId = 1, User = new User { UserId = CurrentUserId } }
                });
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            Assert.True(vm.HasUserReview);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCurrentUserHasNoReview_SetsHasUserReviewFalse()
        {
            _mockReviewService.Setup(s => s.GetReviewsForMovie(_testMovie.MovieId))
                .ReturnsAsync(new List<Review>
                {
                    new() { ReviewId = 1, User = new User { UserId = 999 } } 
                });
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            Assert.False(vm.HasUserReview);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_ClearsStatusMessageAndHidesExtraForm()
        {
            MovieDetailViewModel vm = CreateViewModel();
            vm.StatusMessage = "stale message";
            vm.ShowExtraReviewForm = true;

            await vm.LoadMovieAsync(_testMovie);

            Assert.Equal(string.Empty, vm.StatusMessage);
            Assert.False(vm.ShowExtraReviewForm);
        }

        [Fact]
        public async Task LoadMovieAsync_WhenCalled_LoadsComments()
        {
            MovieDetailViewModel vm = CreateViewModel();

            await vm.LoadMovieAsync(_testMovie);

            _mockCommentService.Verify(s => s.GetCommentsForMovie(_testMovie.MovieId), Times.Once);
        }



        [Fact]
        public async Task SubmitReviewCommand_WhenNoMovieLoaded_DoesNotCallAddReview()
        {
            MovieDetailViewModel vm = CreateViewModel();

            vm.SubmitReviewCommand.Execute(null);
            await Task.Delay(50);

            _mockReviewService.Verify(s => s.AddReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<float>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SubmitReviewCommand_WhenMovieLoaded_CallsAddReviewService()
        {
            _mockReviewService.Setup(s => s.AddReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<float>(), It.IsAny<string>())).Returns(Task.FromResult(new Review()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);

            vm.NewReviewRating = 4.5f;
            vm.NewReviewContent = "Excellent film!";
            vm.SubmitReviewCommand.Execute(null);
            await Task.Delay(100);

            _mockReviewService.Verify(s => s.AddReview(CurrentUserId, _testMovie.MovieId, 4.5f, "Excellent film!"),
                Times.Once);
        }

        [Fact]
        public async Task SubmitReviewCommand_WhenSucceeds_ClearsInputFieldsAndSetsSuccessMessage()
        {
            _mockReviewService.Setup(s => s.AddReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<float>(), It.IsAny<string>())).Returns(Task.FromResult(new Review()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.NewReviewRating = 4.5f;
            vm.NewReviewContent = "Excellent film!";

            string? capturedMessage = null;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusMessage) && !string.IsNullOrEmpty(vm.StatusMessage))
                    capturedMessage = vm.StatusMessage;
            };

            vm.SubmitReviewCommand.Execute(null);
            await Task.Delay(300);

            Assert.Equal(string.Empty, vm.NewReviewContent);
            Assert.Equal(0f, vm.NewReviewRating);
            Assert.Equal("Review submitted successfully!", capturedMessage);
        }

        [Fact]
        public async Task SubmitReviewCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            _mockReviewService.Setup(s => s.AddReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<float>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Already reviewed"));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.NewReviewRating = 4.5f;
            vm.NewReviewContent = "Great!";
            vm.SubmitReviewCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Already reviewed", vm.StatusMessage);
        }



        [Fact]
        public void ShowExtraReviewFormCommand_WhenExecuted_SetsShowExtraReviewFormTrue()
        {
            MovieDetailViewModel vm = CreateViewModel();

            vm.ShowExtraReviewFormCommand.Execute(null);

            Assert.True(vm.ShowExtraReviewForm);
        }



        [Fact]
        public async Task SubmitExtraReviewCommand_WhenUserHasNoRegularReview_SetsStatusMessage()
        {
            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie); 

            vm.SubmitExtraReviewCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("You must submit a regular review first.", vm.StatusMessage);
        }

        [Fact]
        public async Task SubmitExtraReviewCommand_WhenValid_CallsSubmitExtraReviewService()
        {
            Review userReview = new Review { ReviewId = 7, User = new User { UserId = CurrentUserId } };
            _mockReviewService.Setup(s => s.GetReviewsForMovie(_testMovie.MovieId))
                .ReturnsAsync(new List<Review> { userReview });
            _mockReviewService.Setup(s => s.SubmitExtraReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);

            vm.CgiRating = 4; vm.ActingRating = 5;
            vm.PlotRating = 3; vm.SoundRating = 4;
            vm.CinRating = 5;
            vm.SubmitExtraReviewCommand.Execute(null);
            await Task.Delay(100);

            _mockReviewService.Verify(s => s.SubmitExtraReview(userReview.ReviewId,
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SubmitExtraReviewCommand_WhenSucceeds_HidesExtraFormAndSetsSuccessMessage()
        {
            Review userReview = new Review { ReviewId = 7, User = new User { UserId = CurrentUserId } };
            _mockReviewService.Setup(s => s.GetReviewsForMovie(_testMovie.MovieId))
                .ReturnsAsync(new List<Review> { userReview });
            _mockReviewService.Setup(s => s.SubmitExtraReview(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.ShowExtraReviewForm = true;

            string? capturedMessage = null;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusMessage) && !string.IsNullOrEmpty(vm.StatusMessage))
                    capturedMessage = vm.StatusMessage;
            };

            vm.SubmitExtraReviewCommand.Execute(null);
            await Task.Delay(300);

            Assert.False(vm.ShowExtraReviewForm);
            Assert.Equal("Extra review submitted successfully!", capturedMessage);
        }



        [Fact]
        public async Task AddCommentCommand_WhenNoMovieLoaded_DoesNotCallAddComment()
        {
            MovieDetailViewModel vm = CreateViewModel();
            vm.NewCommentContent = "a comment";

            vm.AddCommentCommand.Execute(null);
            await Task.Delay(50);

            _mockCommentService.Verify(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AddCommentCommand_WhenValid_CallsAddCommentService()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.NewCommentContent = "Amazing cinematography!";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            _mockCommentService.Verify(
                s => s.AddComment(CurrentUserId, _testMovie.MovieId, "Amazing cinematography!"), Times.Once);
        }

        [Fact]
        public async Task AddCommentCommand_WhenSucceeds_ClearsNewCommentContent()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.NewCommentContent = "Amazing!";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(string.Empty, vm.NewCommentContent);
        }

        [Fact]
        public async Task AddCommentCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Rate limited"));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.NewCommentContent = "A comment";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Rate limited", vm.StatusMessage);
        }



        [Fact]
        public async Task DeleteReviewCommand_WhenMovieLoaded_CallsDeleteReviewService()
        {
            _mockReviewService.Setup(s => s.DeleteReview(It.IsAny<int>())).Returns(Task.CompletedTask);

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);

            vm.DeleteReviewCommand.Execute(5);
            await Task.Delay(100);

            _mockReviewService.Verify(s => s.DeleteReview(5), Times.Once);
        }

    [Fact]
    public async Task DeleteReviewCommand_WhenSucceeds_SetsDeletedStatusMessage()
    {
        _mockReviewService.Setup(s => s.DeleteReview(It.IsAny<int>())).Returns(Task.CompletedTask);

        MovieDetailViewModel vm = CreateViewModel();
        await vm.LoadMovieAsync(_testMovie);

        string? capturedMessage = null;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.StatusMessage) && vm.StatusMessage != string.Empty)
                capturedMessage = vm.StatusMessage;
        };

        vm.DeleteReviewCommand.Execute(5);
        await Task.Delay(200);

        Assert.Equal("Review deleted.", capturedMessage);
    }

        [Fact]
        public async Task DeleteReviewCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            _mockReviewService.Setup(s => s.DeleteReview(It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Not found"));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.DeleteReviewCommand.Execute(5);
            await Task.Delay(100);

            Assert.Equal("Not found", vm.StatusMessage);
        }


        [Fact]
        public void BackCommand_WhenExecuted_RaisesNavigateBackEvent()
        {
            MovieDetailViewModel vm = CreateViewModel();
            bool raised = false;
            vm.NavigateBack += () => raised = true;

            vm.BackCommand.Execute(null);

            Assert.True(raised);
        }


        [Fact]
        public void StartReplyCommand_WhenCommentIdPassed_SetsReplyToCommentId()
        {
            MovieDetailViewModel vm = CreateViewModel();

            vm.StartReplyCommand.Execute(99);

            Assert.Equal(99, vm.ReplyToCommentId);
        }

        [Fact]
        public void CancelReplyCommand_WhenExecuted_ClearsReplyState()
        {
            MovieDetailViewModel vm = CreateViewModel();
            vm.ReplyToCommentId = 5;
            vm.ReplyContent = "half typed";

            vm.CancelReplyCommand.Execute(null);

            Assert.Equal(0, vm.ReplyToCommentId);
            Assert.Equal(string.Empty, vm.ReplyContent);
        }

        [Fact]
        public async Task SubmitReplyCommand_WhenValid_CallsAddReplyService()
        {
            _mockCommentService.Setup(s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.ReplyToCommentId = 7;
            vm.ReplyContent = "Great point!";
            vm.SubmitReplyCommand.Execute(null);
            await Task.Delay(100);

            _mockCommentService.Verify(s => s.AddReply(CurrentUserId, 7, "Great point!"), Times.Once);
        }

        [Fact]
        public async Task SubmitReplyCommand_WhenSucceeds_ClearsReplyFields()
        {
            _mockCommentService.Setup(s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            MovieDetailViewModel vm = CreateViewModel();
            await vm.LoadMovieAsync(_testMovie);
            vm.ReplyToCommentId = 7;
            vm.ReplyContent = "Agreed!";
            vm.SubmitReplyCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(string.Empty, vm.ReplyContent);
            Assert.Equal(0, vm.ReplyToCommentId);
        }

        [Fact]
        public async Task SubmitReplyCommand_WhenNoMovieLoaded_DoesNotCallAddReply()
        {
            MovieDetailViewModel vm = CreateViewModel();
            vm.ReplyToCommentId = 7;
            vm.ReplyContent = "A reply";

            vm.SubmitReplyCommand.Execute(null);
            await Task.Delay(50);

            _mockCommentService.Verify(
                s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}