#nullable enable
using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces;

/// <summary>
/// Service interface for comment/forum operations.
/// </summary>
public interface ICommentService
{
    /// <summary>Gets all comments for a movie, ordered by creation date descending.</summary>
    Task<List<Comment>> GetCommentsForMovie(int movieId);

    /// <summary>Adds a root-level comment on a movie.</summary>
    Task<Comment> AddComment(int userId, int movieId, string content);

    /// <summary>Adds a reply to an existing comment.</summary>
    Task<Comment> AddReply(int userId, int parentCommentId, string content);

    /// <summary>Deletes a comment.</summary>
    Task DeleteComment(int commentId);
}
