#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly string _connectionString;

    public ReviewRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual List<Review> GetAll()
    {
        var reviews = new List<Review>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT ReviewId, UserId, MovieId, StarRating, Content, CreatedAt, IsExtraReview,
                   CinematographyRating, CinematographyText, ActingRating, ActingText,
                   CgiRating, CgiText, PlotRating, PlotText, SoundRating, SoundText
            FROM Review", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            reviews.Add(MapReview(reader));
        }

        return reviews;
    }

    public virtual Review? GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT ReviewId, UserId, MovieId, StarRating, Content, CreatedAt, IsExtraReview,
                   CinematographyRating, CinematographyText, ActingRating, ActingText,
                   CgiRating, CgiText, PlotRating, PlotText, SoundRating, SoundText
            FROM Review
            WHERE ReviewId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapReview(reader);
    }

    public virtual int Insert(Review review)
    {
        if (review.User is null)
            throw new InvalidOperationException("Review.User is required for insert.");
        if (review.Movie is null)
            throw new InvalidOperationException("Review.Movie is required for insert.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Review (UserId, MovieId, StarRating, Content, CreatedAt, IsExtraReview,
                                 CinematographyRating, CinematographyText, ActingRating, ActingText,
                                 CgiRating, CgiText, PlotRating, PlotText, SoundRating, SoundText)
            VALUES (@userId, @movieId, @starRating, @content, @createdAt, @isExtraReview,
                    @cinematographyRating, @cinematographyText, @actingRating, @actingText,
                    @cgiRating, @cgiText, @plotRating, @plotText, @soundRating, @soundText);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@userId", review.User.UserId);
        cmd.Parameters.AddWithValue("@movieId", review.Movie.MovieId);
        cmd.Parameters.AddWithValue("@starRating", review.StarRating);
        cmd.Parameters.AddWithValue("@content", review.Content);
        cmd.Parameters.AddWithValue("@createdAt", review.CreatedAt);
        cmd.Parameters.AddWithValue("@isExtraReview", review.IsExtraReview);
        cmd.Parameters.AddWithValue("@cinematographyRating", review.CinematographyRating == 0 ? (object)DBNull.Value : review.CinematographyRating);
        cmd.Parameters.AddWithValue("@cinematographyText", (object?)review.CinematographyText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actingRating", review.ActingRating == 0 ? (object)DBNull.Value : review.ActingRating);
        cmd.Parameters.AddWithValue("@actingText", (object?)review.ActingText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cgiRating", review.CgiRating == 0 ? (object)DBNull.Value : review.CgiRating);
        cmd.Parameters.AddWithValue("@cgiText", (object?)review.CgiText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@plotRating", review.PlotRating == 0 ? (object)DBNull.Value : review.PlotRating);
        cmd.Parameters.AddWithValue("@plotText", (object?)review.PlotText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@soundRating", review.SoundRating == 0 ? (object)DBNull.Value : review.SoundRating);
        cmd.Parameters.AddWithValue("@soundText", (object?)review.SoundText ?? DBNull.Value);

        connection.Open();
        var id = (int)cmd.ExecuteScalar()!;
        review.ReviewId = id;
        return id;
    }

    public virtual bool Update(Review review)
    {
        if (review.User is null)
            throw new InvalidOperationException("Review.User is required for update.");
        if (review.Movie is null)
            throw new InvalidOperationException("Review.Movie is required for update.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Review
            SET UserId = @userId,
                MovieId = @movieId,
                StarRating = @starRating,
                Content = @content,
                CreatedAt = @createdAt,
                IsExtraReview = @isExtraReview,
                CinematographyRating = @cinematographyRating,
                CinematographyText = @cinematographyText,
                ActingRating = @actingRating,
                ActingText = @actingText,
                CgiRating = @cgiRating,
                CgiText = @cgiText,
                PlotRating = @plotRating,
                PlotText = @plotText,
                SoundRating = @soundRating,
                SoundText = @soundText
            WHERE ReviewId = @reviewId", connection);

        cmd.Parameters.AddWithValue("@reviewId", review.ReviewId);
        cmd.Parameters.AddWithValue("@userId", review.User.UserId);
        cmd.Parameters.AddWithValue("@movieId", review.Movie.MovieId);
        cmd.Parameters.AddWithValue("@starRating", review.StarRating);
        cmd.Parameters.AddWithValue("@content", review.Content);
        cmd.Parameters.AddWithValue("@createdAt", review.CreatedAt);
        cmd.Parameters.AddWithValue("@isExtraReview", review.IsExtraReview);
        cmd.Parameters.AddWithValue("@cinematographyRating", review.CinematographyRating);
        cmd.Parameters.AddWithValue("@cinematographyText", (object?)review.CinematographyText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actingRating", review.ActingRating);
        cmd.Parameters.AddWithValue("@actingText", (object?)review.ActingText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cgiRating", review.CgiRating);
        cmd.Parameters.AddWithValue("@cgiText", (object?)review.CgiText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@plotRating", review.PlotRating);
        cmd.Parameters.AddWithValue("@plotText", (object?)review.PlotText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@soundRating", review.SoundRating);
        cmd.Parameters.AddWithValue("@soundText", (object?)review.SoundText ?? DBNull.Value);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public virtual bool Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM Review WHERE ReviewId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    private static Review MapReview(SqlDataReader reader)
    {
        return new Review
        {
            ReviewId = reader.GetInt32(reader.GetOrdinal("ReviewId")),
            StarRating = Convert.ToSingle(reader["StarRating"]),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            IsExtraReview = reader.GetBoolean(reader.GetOrdinal("IsExtraReview")),
            CinematographyRating = reader.IsDBNull(reader.GetOrdinal("CinematographyRating")) ? 0 : reader.GetInt32(reader.GetOrdinal("CinematographyRating")),
            CinematographyText = reader.IsDBNull(reader.GetOrdinal("CinematographyText"))
                ? null
                : reader.GetString(reader.GetOrdinal("CinematographyText")),
            ActingRating = reader.IsDBNull(reader.GetOrdinal("ActingRating")) ? 0 : reader.GetInt32(reader.GetOrdinal("ActingRating")),
            ActingText = reader.IsDBNull(reader.GetOrdinal("ActingText"))
                ? null
                : reader.GetString(reader.GetOrdinal("ActingText")),
            CgiRating = reader.IsDBNull(reader.GetOrdinal("CgiRating")) ? 0 : reader.GetInt32(reader.GetOrdinal("CgiRating")),
            CgiText = reader.IsDBNull(reader.GetOrdinal("CgiText"))
                ? null
                : reader.GetString(reader.GetOrdinal("CgiText")),
            PlotRating = reader.IsDBNull(reader.GetOrdinal("PlotRating")) ? 0 : reader.GetInt32(reader.GetOrdinal("PlotRating")),
            PlotText = reader.IsDBNull(reader.GetOrdinal("PlotText"))
                ? null
                : reader.GetString(reader.GetOrdinal("PlotText")),
            SoundRating = reader.IsDBNull(reader.GetOrdinal("SoundRating")) ? 0 : reader.GetInt32(reader.GetOrdinal("SoundRating")),
            SoundText = reader.IsDBNull(reader.GetOrdinal("SoundText"))
                ? null
                : reader.GetString(reader.GetOrdinal("SoundText")),
            User = new User { UserId = reader.GetInt32(reader.GetOrdinal("UserId")) },
            Movie = new Movie { MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")) }
        };
    }
}