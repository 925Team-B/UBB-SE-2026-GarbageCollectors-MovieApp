#nullable enable
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class CommentServiceTests
{
    private readonly Mock<ICommentRepository> _commentRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IMovieRepository> _movieRepoMock;
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _commentRepoMock = new Mock<ICommentRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _movieRepoMock = new Mock<IMovieRepository>();

        _sut = new CommentService(
            _commentRepoMock.Object,
            _userRepoMock.Object,
            _movieRepoMock.Object);
    }

    // --- GetCommentsForMovie ---

    [Fact]
    public async Task GetCommentsForMovie_WhenCommentsExist_ReturnsCommentsOrderedByDateDescending()
    {
        var movie = new Movie { MovieId = 1 };
        var earlier = new Comment { MessageId = 1, Movie = movie, Content = "First", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var later = new Comment { MessageId = 2, Movie = movie, Content = "Second", CreatedAt = DateTime.UtcNow };
        _commentRepoMock.Setup(r => r.GetAll()).Returns(new List<Comment> { earlier, later });

        var result = await _sut.GetCommentsForMovie(1);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].MessageId); // most recent first
    }

    [Fact]
    public async Task GetCommentsForMovie_WhenNoCommentsForMovie_ReturnsEmptyList()
    {
        var comment = new Comment { MessageId = 1, Movie = new Movie { MovieId = 99 }, CreatedAt = DateTime.UtcNow };
        _commentRepoMock.Setup(r => r.GetAll()).Returns(new List<Comment> { comment });

        var result = await _sut.GetCommentsForMovie(1);

        Assert.Empty(result);
    }

    // --- AddComment ---

    [Fact]
    public async Task AddComment_WhenValidInputs_ReturnsCreatedComment()
    {
        var user = new User { UserId = 1 };
        var movie = new Movie { MovieId = 1 };
        _userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie);

        var result = await _sut.AddComment(1, 1, "A valid comment");

        Assert.Equal("A valid comment", result.Content);
        Assert.Null(result.ParentComment);
        _commentRepoMock.Verify(r => r.Insert(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task AddComment_WhenContentExceedsMaxLength_ThrowsInvalidOperationException()
    {
        var longContent = new string('x', 10001);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddComment(1, 1, longContent));
    }

    [Fact]
    public async Task AddComment_WhenUserNotFound_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddComment(99, 1, "Hello"));
    }

    [Fact]
    public async Task AddComment_WhenMovieNotFound_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(r => r.GetById(1)).Returns(new User { UserId = 1 });
        _movieRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Movie?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddComment(1, 99, "Hello"));
    }

    // --- AddReply ---

    [Fact]
    public async Task AddReply_WhenParentCommentExists_ReturnsReplyWithParentSet()
    {
        var parentComment = new Comment { MessageId = 5, Movie = new Movie { MovieId = 1 } };
        var user = new User { UserId = 1 };
        var movie = new Movie { MovieId = 1 };

        _commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        _userRepoMock.Setup(r => r.GetById(1)).Returns(user);
        _movieRepoMock.Setup(r => r.GetById(1)).Returns(movie);

        var result = await _sut.AddReply(1, 5, "Nice reply");

        Assert.Equal("Nice reply", result.Content);
        Assert.NotNull(result.ParentComment);
        Assert.Equal(5, result.ParentComment.MessageId);
        _commentRepoMock.Verify(r => r.Insert(It.IsAny<Comment>()), Times.Once);
    }

    [Fact]
    public async Task AddReply_WhenParentCommentNotFound_ThrowsInvalidOperationException()
    {
        _commentRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Comment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddReply(1, 999, "reply"));
    }

    [Fact]
    public async Task AddReply_WhenContentExceedsMaxLength_ThrowsInvalidOperationException()
    {
        var parentComment = new Comment { MessageId = 5, Movie = new Movie { MovieId = 1 } };
        _commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        var longContent = new string('x', 10001);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddReply(1, 5, longContent));
    }

    [Fact]
    public async Task AddReply_WhenParentCommentHasNoMovie_ThrowsInvalidOperationException()
    {
        var parentComment = new Comment { MessageId = 5, Movie = null };
        _commentRepoMock.Setup(r => r.GetById(5)).Returns(parentComment);
        _userRepoMock.Setup(r => r.GetById(1)).Returns(new User { UserId = 1 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddReply(1, 5, "reply"));
    }

    // --- DeleteComment ---

    [Fact]
    public async Task DeleteComment_WhenCommentExists_CallsRepositoryDelete()
    {
        var comment = new Comment { MessageId = 10 };
        _commentRepoMock.Setup(r => r.GetById(10)).Returns(comment);

        await _sut.DeleteComment(10);

        _commentRepoMock.Verify(r => r.Delete(10), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_WhenCommentNotFound_ThrowsInvalidOperationException()
    {
        _commentRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Comment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteComment(999));
    }
}
