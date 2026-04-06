#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly string _connectionString;

    public CommentRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<Comment> GetAll()
    {
        var comments = new List<Comment>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT MessageId, AuthorId, MovieId, ParentCommentId, Content, CreatedAt
            FROM Comment", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            comments.Add(MapComment(reader));
        }

        return comments;
    }

    public Comment? GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT MessageId, AuthorId, MovieId, ParentCommentId, Content, CreatedAt
            FROM Comment
            WHERE MessageId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapComment(reader);
    }

    public int Insert(Comment comment)
    {
        if (comment.Author is null)
            throw new InvalidOperationException("Comment.Author is required for insert.");
        if (comment.Movie is null)
            throw new InvalidOperationException("Comment.Movie is required for insert.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Comment (AuthorId, MovieId, ParentCommentId, Content, CreatedAt)
            VALUES (@authorId, @movieId, @parentCommentId, @content, @createdAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@authorId", comment.Author.UserId);
        cmd.Parameters.AddWithValue("@movieId", comment.Movie.MovieId);
        cmd.Parameters.AddWithValue("@parentCommentId", (object?)comment.ParentComment?.MessageId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@content", comment.Content);
        cmd.Parameters.AddWithValue("@createdAt", comment.CreatedAt);

        connection.Open();
        var id = (int)cmd.ExecuteScalar()!;
        comment.MessageId = id;
        return id;
    }

    public bool Update(Comment comment)
    {
        if (comment.Author is null)
            throw new InvalidOperationException("Comment.Author is required for update.");
        if (comment.Movie is null)
            throw new InvalidOperationException("Comment.Movie is required for update.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Comment
            SET AuthorId = @authorId,
                MovieId = @movieId,
                ParentCommentId = @parentCommentId,
                Content = @content,
                CreatedAt = @createdAt
            WHERE MessageId = @id", connection);

        cmd.Parameters.AddWithValue("@id", comment.MessageId);
        cmd.Parameters.AddWithValue("@authorId", comment.Author.UserId);
        cmd.Parameters.AddWithValue("@movieId", comment.Movie.MovieId);
        cmd.Parameters.AddWithValue("@parentCommentId", (object?)comment.ParentComment?.MessageId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@content", comment.Content);
        cmd.Parameters.AddWithValue("@createdAt", comment.CreatedAt);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM Comment WHERE MessageId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    private static Comment MapComment(SqlDataReader reader)
    {

        var parentCommentIdOrdinal = reader.GetOrdinal("ParentCommentId");

        return new Comment
        {
            MessageId = reader.GetInt32(reader.GetOrdinal("MessageId")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            Author = new User { UserId = reader.GetInt32(reader.GetOrdinal("AuthorId")) },
            Movie = new Movie { MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")) },
            ParentComment = reader.IsDBNull(parentCommentIdOrdinal)
                ? null
                : new Comment { MessageId = reader.GetInt32(parentCommentIdOrdinal) }
        };
    }
}
