#nullable enable
using MovieApp.Core.Interfaces;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;

namespace MovieApp.Core.Services;

/// <summary>
/// Service for comment/forum operations including threaded replies.
/// </summary>
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMovieRepository _movieRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="CommentService"/>.
    /// </summary>
    /// <param name="commentRepository">The comment repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="movieRepository">The movie repository.</param>
    public CommentService(
        CommentRepository commentRepository,
        UserRepository userRepository,
        MovieRepository movieRepository)
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _movieRepository = movieRepository;
    }

    /// <summary>
    /// Gets all comments for a movie, ordered by creation date descending.
    /// Returns a flat list (UI builds the tree).
    /// </summary>
    /// <param name="movieId">The movie identifier.</param>
    /// <returns>A flat list of comments.</returns>
    public async Task<List<Comment>> GetCommentsForMovie(int movieId)
    {
        var comments = _commentRepository.GetAll()
            .Where(c => c.Movie?.MovieId == movieId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        return await Task.FromResult(comments);
    }

    /// <summary>
    /// Adds a root-level comment on a movie.
    /// </summary>
    /// <param name="userId">The comment author's ID.</param>
    /// <param name="movieId">The movie's ID.</param>
    /// <param name="content">The comment content (max 10000 chars).</param>
    /// <returns>The created comment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when content exceeds max length.</exception>
    public async Task<Comment> AddComment(int userId, int movieId, string content)
    {
        if (!string.IsNullOrEmpty(content) && content.Length > 10000)
            throw new InvalidOperationException("Comment content must not exceed 10000 characters.");

        var author = _userRepository.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");
        var movie = _movieRepository.GetById(movieId)
            ?? throw new InvalidOperationException("Movie not found.");

        var comment = new Comment
        {
            Author = author,
            Movie = movie,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ParentComment = null
        };

        _commentRepository.Insert(comment);

        return comment;
    }

    /// <summary>
    /// Adds a reply to an existing comment. Inherits MovieId from parent.
    /// </summary>
    /// <param name="userId">The reply author's ID.</param>
    /// <param name="parentCommentId">The parent comment's ID.</param>
    /// <param name="content">The reply content (max 10000 chars).</param>
    /// <returns>The created reply comment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when parent not found or content invalid.</exception>
    public async Task<Comment> AddReply(int userId, int parentCommentId, string content)
    {
        var parentComment = _commentRepository.GetById(parentCommentId)
            ?? throw new InvalidOperationException("Parent comment not found.");

        if (!string.IsNullOrEmpty(content) && content.Length > 10000)
            throw new InvalidOperationException("Comment content must not exceed 10000 characters.");

        var author = _userRepository.GetById(userId)
            ?? throw new InvalidOperationException("User not found.");
        var parentMovieId = parentComment.Movie?.MovieId
            ?? throw new InvalidOperationException("Parent comment movie is not available.");
        var movie = _movieRepository.GetById(parentMovieId)
            ?? throw new InvalidOperationException("Movie not found.");

        var reply = new Comment
        {
            Author = author,
            Movie = movie,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            ParentComment = new Comment { MessageId = parentCommentId }
        };

        _commentRepository.Insert(reply);

        return reply;
    }

    /// <summary>
    /// Deletes a comment by its ID.
    /// </summary>
    /// <param name="commentId">The comment identifier.</param>
    public async Task DeleteComment(int commentId)
    {
        var comment = _commentRepository.GetById(commentId)
            ?? throw new InvalidOperationException("Comment not found.");

        _commentRepository.Delete(comment.MessageId);
        await Task.CompletedTask;
    }
}
