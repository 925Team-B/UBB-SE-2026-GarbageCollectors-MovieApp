#nullable enable
using Moq;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> commentRepoMock;
    private readonly Mock<IUserRepository> userRepoMock;
    private readonly Mock<IMovieRepository> movieRepoMock;
    private readonly CommentService sut;

    public CommentServiceTests()
    {
        commentRepoMock = new Mock<ICommentRepository>();
        userRepoMock = new Mock<IUserRepository>();
        movieRepoMock = new Mock<IMovieRepository>();

        sut = new CommentService(
            commentRepoMock.Object,
            userRepoMock.Object,
            movieRepoMock.Object);
    }

    // --- GetCommentsForMovie ---
    [Fact]
    public async Task GetCommentsForMovie_WhenCommentsExist_ReturnsCommentsOrderedByDateDescending()
    {
        var movie = new Movie { MovieId = 1 };
        var earlier = new Comment { MessageId = 1, Movie = movie, Content = "First", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var later = new Comment { MessageId = 2, Movie = movie, Content = "Second", CreatedAt = DateTime.UtcNow };
        commentRepoMock.Setup(r => r.GetAll()).Returns(new List<Comment> { earlier, later });

        var result = await sut.GetCommentsForMovie(1);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].MessageId); // most recent first
    }

    [Fact]
    public async Task GetCommentsForMovie_WhenNoCommentsForMovie_ReturnsEmptyList()
    {
        var comment = new Comment { MessageId = 1, Movie = new Movie { MovieId = 99 }, CreatedAt = DateTime.UtcNow };
        commentRepoMock.Setup(r => r.GetAll()).Returns(new List<Comment> { comment });

        var result = await sut.GetCommentsForMovie(1);

        Assert.Empty(result);
    }

    // --- AddComment ---
    [Fact]
    public async Task AddComment_WhenValidInputs_ReturnsCreatedComment()
    {
        var user = new User { UserId = 1 };
        var movie = new Movie { MovieId = 1 };
        userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie);

        var result = await sut.AddComment(1, 1, "A valid comment");

        Assert.Equal("A valid comment", result.Content);
        Assert.Null(result.ParentComment);
        commentRepoMock.Verify(r => r.Insert(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_WhenContentExceedsMaxLength_ThrowsInvalidOperationException()
    {
        var longContent = new string('x', 10001);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddComment(1, 1, longContent));
    }

    [Fact]
    public async Task AddComment_WhenUserNotFound_ThrowsInvalidOperationException()
    {
        userRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddComment(99, 1, "Hello"));
    }

    [Fact]
    public async Task AddComment_WhenMovieNotFound_ThrowsInvalidOperationException()
    {
        userRepoMock.Setup(r => r.GetById(1)).Returns(new User { UserId = 1 });
        movieRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Movie?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddComment(1, 99, "Hello"));
    }

    // --- AddReply ---
    [Fact]
    public async Task AddReply_WhenParentCommentExists_ReturnsReplyWithParentSet()
    {
        var parentComment = new Comment { MessageId = 5, Movie = new Movie { MovieId = 1 } };
        var user = new User { UserId = 1 };
        var movie = new Movie { MovieId = 1 };

        commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        movieRepoMock.Setup(r => r.GetById(1)).Returns(movie);

        var result = await sut.AddReply(1, 5, "Nice reply");

        Assert.Equal("Nice reply", result.Content);
        Assert.NotNull(result.ParentComment);
        Assert.Equal(5, result.ParentComment.MessageId);
        commentRepoMock.Verify(r => r.Insert(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task AddReply_WhenParentCommentNotFound_ThrowsInvalidOperationException()
    {
        commentRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Comment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddReply(1, 999, "reply"));
    }

    [Fact]
    public async Task AddReply_WhenContentExceedsMaxLength_ThrowsInvalidOperationException()
    {
        var parentComment = new Comment { MessageId = 5, Movie = new Movie { MovieId = 1 } };
        commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        var longContent = new string('x', 10001);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddReply(1, 5, longContent));
    }

    [Fact]
    public async Task AddReply_WhenParentCommentHasNoMovie_ThrowsInvalidOperationException()
    {
        var parentComment = new Comment { MessageId = 5, Movie = null };
        commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        userRepoMock.Setup(r => r.GetById(1)).Returns(new User { UserId = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddReply(1, 5, "reply"));
    }

    // --- DeleteComment ---
    [Fact]
    public async Task DeleteComment_WhenCommentExists_CallsRepositoryDelete()
    {
        var comment = new Comment { MessageId = 10 };
        commentRepoMock.Setup(r => r.GetById(10)).Returns(comment);

        await sut.DeleteComment(10);

        commentRepoMock.Verify(r => r.Delete(10), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_WhenCommentNotFound_ThrowsInvalidOperationException()
    {
        commentRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Comment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteComment(999));
    }
}
