using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieApp.UI.ViewModels;
using MovieApp.Core.Models;
using MovieApp.Core.Interfaces;

namespace MovieApp.Tests.Unit.ViewModels
{
    public class ForumViewModelTests
    {
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly Mock<ICatalogService> _mockCatalogService;
        private const int CurrentUserId = 1;

        public ForumViewModelTests()
        {
            _mockCommentService = new Mock<ICommentService>();
            _mockCatalogService = new Mock<ICatalogService>();

            _mockCatalogService
                .Setup(s => s.GetAllMovies())
                .ReturnsAsync(new List<Movie>());

            _mockCommentService
                .Setup(s => s.GetCommentsForMovie(It.IsAny<int>()))
                .ReturnsAsync(new List<Comment>());
        }

        private ForumViewModel CreateViewModel() =>
            new(_mockCommentService.Object, _mockCatalogService.Object, CurrentUserId);



        [Fact]
        public async Task LoadMoviesAsync_WhenCalled_CallsGetAllMovies()
        {
            ForumViewModel vm = CreateViewModel();

            await vm.LoadMoviesAsync();

            _mockCatalogService.Verify(s => s.GetAllMovies(), Times.Once);
        }

        [Fact]
        public async Task LoadMoviesAsync_WhenMoviesReturned_PopulatesMoviesCollection()
        {
            _mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>
            {
                new() { MovieId = 1, Title = "Inception" },
                new() { MovieId = 2, Title = "Interstellar" }
            });

            ForumViewModel vm = CreateViewModel();
            await vm.LoadMoviesAsync();

            Assert.Equal(2, vm.Movies.Count);
        }

        [Fact]
        public async Task LoadMoviesAsync_WhenMoviesLoadedAndNoMovieSelected_SelectsFirstMovie()
        {
            Movie firstMovie = new Movie { MovieId = 5, Title = "First" };
            _mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>
            {
                firstMovie,
                new() { MovieId = 6, Title = "Second" }
            });
            _mockCommentService.Setup(s => s.GetCommentsForMovie(5)).ReturnsAsync(new List<Comment>());

            ForumViewModel vm = CreateViewModel();
            await vm.LoadMoviesAsync();

            Assert.Equal(firstMovie, vm.SelectedMovie);
        }

        [Fact]
        public async Task LoadMoviesAsync_WhenMoviesLoadedAndMovieAlreadySelected_DoesNotChangeSelection()
        {
            _mockCatalogService.Setup(s => s.GetAllMovies()).ReturnsAsync(new List<Movie>
            {
                new() { MovieId = 5 },
                new() { MovieId = 6 }
            });
            _mockCommentService.Setup(s => s.GetCommentsForMovie(It.IsAny<int>())).ReturnsAsync(new List<Comment>());

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 10; 
            await vm.LoadMoviesAsync();

            Assert.Equal(10, vm.SelectedMovieId);
        }



        [Fact]
        public async Task LoadCommentsAsync_WhenMovieSelected_CallsGetCommentsForMovie()
        {
            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 3;
            await Task.Delay(50);

            _mockCommentService.Verify(s => s.GetCommentsForMovie(3), Times.AtLeastOnce);
        }

        [Fact]
        public async Task LoadCommentsAsync_WhenMovieIdIsZero_DoesNotCallService()
        {
            ForumViewModel vm = CreateViewModel();
            await vm.LoadCommentsAsync();

            _mockCommentService.Verify(s => s.GetCommentsForMovie(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LoadCommentsAsync_WhenRootCommentsReturned_PopulatesRootCommentsCollection()
        {
            Comment rootComment = new Comment { MessageId = 1, Content = "Root", ParentComment = null };
            _mockCommentService.Setup(s => s.GetCommentsForMovie(5))
                .ReturnsAsync(new List<Comment> { rootComment });

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            await Task.Delay(100);

            Assert.Single(vm.RootComments);
        }



        [Fact]
        public async Task SelectedMovie_WhenSet_UpdatesSelectedMovieId()
        {
            Movie movie = new Movie { MovieId = 7, Title = "Dune" };
            _mockCommentService.Setup(s => s.GetCommentsForMovie(7)).ReturnsAsync(new List<Comment>());

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovie = movie;
            await Task.Delay(50);

            Assert.Equal(7, vm.SelectedMovieId);
        }

        [Fact]
        public async Task SelectedMovieId_WhenChanged_TriggersLoadComments()
        {
            _mockCommentService.Setup(s => s.GetCommentsForMovie(9)).ReturnsAsync(new List<Comment>());

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 9;
            await Task.Delay(100);

            _mockCommentService.Verify(s => s.GetCommentsForMovie(9), Times.AtLeastOnce);
        }


        [Fact]
        public async Task AddCommentCommand_WhenNoMovieSelected_SetsValidationStatusMessage()
        {
            ForumViewModel vm = CreateViewModel();
            vm.NewCommentContent = "A comment";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("Please select a movie and enter comment content.", vm.StatusMessage);
        }

        [Fact]
        public async Task AddCommentCommand_WhenContentEmpty_SetsValidationStatusMessage()
        {
            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.NewCommentContent = string.Empty;
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("Please select a movie and enter comment content.", vm.StatusMessage);
        }

        [Fact]
        public async Task AddCommentCommand_WhenValid_CallsAddCommentService()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.NewCommentContent = "Great movie!";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            _mockCommentService.Verify(s => s.AddComment(CurrentUserId, 5, "Great movie!"), Times.Once);
        }

        [Fact]
        public async Task AddCommentCommand_WhenSucceeds_ClearsNewCommentContentAndSetsStatusMessage()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.NewCommentContent = "Great movie!";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(string.Empty, vm.NewCommentContent);
            Assert.Equal("Comment posted!", vm.StatusMessage);
        }

        [Fact]
        public async Task AddCommentCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            _mockCommentService.Setup(s => s.AddComment(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Forbidden"));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.NewCommentContent = "A comment";
            vm.AddCommentCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Forbidden", vm.StatusMessage);
        }



        [Fact]
        public async Task ReplyCommand_WhenNoReplyToCommentId_SetsValidationStatusMessage()
        {
            ForumViewModel vm = CreateViewModel();
            vm.ReplyToCommentId = 0;
            vm.ReplyContent = "A reply";
            vm.ReplyCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("Please enter reply content.", vm.StatusMessage);
        }

        [Fact]
        public async Task ReplyCommand_WhenReplyContentEmpty_SetsValidationStatusMessage()
        {
            ForumViewModel vm = CreateViewModel();
            vm.ReplyToCommentId = 1;
            vm.ReplyContent = string.Empty;
            vm.ReplyCommand.Execute(null);
            await Task.Delay(50);

            Assert.Equal("Please enter reply content.", vm.StatusMessage);
        }

        [Fact]
        public async Task ReplyCommand_WhenValid_CallsAddReplyService()
        {
            _mockCommentService.Setup(s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.ReplyToCommentId = 10;
            vm.ReplyContent = "Nice reply!";
            vm.ReplyCommand.Execute(null);
            await Task.Delay(100);

            _mockCommentService.Verify(s => s.AddReply(CurrentUserId, 10, "Nice reply!"), Times.Once);
        }

        [Fact]
        public async Task ReplyCommand_WhenSucceeds_ClearsReplyFieldsAndSetsStatusMessage()
        {
            _mockCommentService.Setup(s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new Comment()));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.ReplyToCommentId = 10;
            vm.ReplyContent = "Nice reply!";
            vm.ReplyCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal(string.Empty, vm.ReplyContent);
            Assert.Equal(0, vm.ReplyToCommentId);
            Assert.Equal("Reply posted!", vm.StatusMessage);
        }

        [Fact]
        public async Task ReplyCommand_WhenServiceThrowsInvalidOperation_SetsStatusMessage()
        {
            _mockCommentService.Setup(s => s.AddReply(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Already replied"));

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            vm.ReplyToCommentId = 10;
            vm.ReplyContent = "A reply";
            vm.ReplyCommand.Execute(null);
            await Task.Delay(100);

            Assert.Equal("Already replied", vm.StatusMessage);
        }



        [Fact]
        public void StartReplyCommand_WhenCommentIdPassed_SetsReplyToCommentId()
        {
            ForumViewModel vm = CreateViewModel();

            vm.StartReplyCommand.Execute(42);

            Assert.Equal(42, vm.ReplyToCommentId);
        }

        [Fact]
        public void CancelReplyCommand_WhenExecuted_ClearsReplyContentAndReplyToCommentId()
        {
            ForumViewModel vm = CreateViewModel();
            vm.ReplyContent = "half written";
            vm.ReplyToCommentId = 7;

            vm.CancelReplyCommand.Execute(null);

            Assert.Equal(string.Empty, vm.ReplyContent);
            Assert.Equal(0, vm.ReplyToCommentId);
        }



        [Fact]
        public async Task LoadCommentsAsync_WhenReplyCommentPresent_NestedUnderParentNotInRootComments()
        {
            Comment parent = new Comment { MessageId = 1, Content = "Root", ParentComment = null };
            Comment reply = new Comment { MessageId = 2, Content = "Reply", ParentComment = parent };

            _mockCommentService.Setup(s => s.GetCommentsForMovie(5))
                .ReturnsAsync(new List<Comment> { parent, reply });

            ForumViewModel vm = CreateViewModel();
            vm.SelectedMovieId = 5;
            await Task.Delay(100);

            Assert.Single(vm.RootComments);           
            Assert.Equal(2, vm.Comments.Count);       
        }
    }
}