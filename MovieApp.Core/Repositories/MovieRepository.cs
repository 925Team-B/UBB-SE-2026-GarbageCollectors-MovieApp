#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class MovieRepository
{
    private readonly string _connectionString;

    public MovieRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual List<Movie> GetAll()
    {
        var movies = new List<Movie>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT MovieId, Title, [Year], PosterUrl, Genre, AverageRating
            FROM Movie", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            movies.Add(new Movie
            {
                MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Year = reader.GetInt32(reader.GetOrdinal("Year")),
                PosterUrl = reader.IsDBNull(reader.GetOrdinal("PosterUrl")) ? string.Empty : reader.GetString(reader.GetOrdinal("PosterUrl")),
                Genre = reader.IsDBNull(reader.GetOrdinal("Genre")) ? string.Empty : reader.GetString(reader.GetOrdinal("Genre")),
                AverageRating = reader.GetDouble(reader.GetOrdinal("AverageRating"))
            });
        }

        return movies;
    }

    public virtual Movie? GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT MovieId, Title, [Year], PosterUrl, Genre, AverageRating
            FROM Movie
            WHERE MovieId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Movie
        {
            MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Year = reader.GetInt32(reader.GetOrdinal("Year")),
            PosterUrl = reader.GetString(reader.GetOrdinal("PosterUrl")),
            Genre = reader.GetString(reader.GetOrdinal("Genre")),
            AverageRating = reader.GetDouble(reader.GetOrdinal("AverageRating"))
        };
    }

    public virtual int Insert(Movie movie)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Movie (Title, [Year], PosterUrl, Genre, AverageRating)
            VALUES (@title, @year, @posterUrl, @genre, @averageRating);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@title", movie.Title);
        cmd.Parameters.AddWithValue("@year", movie.Year);
        cmd.Parameters.AddWithValue("@posterUrl", movie.PosterUrl);
        cmd.Parameters.AddWithValue("@genre", movie.Genre);
        cmd.Parameters.AddWithValue("@averageRating", movie.AverageRating);

        connection.Open();
        var id = (int)cmd.ExecuteScalar()!;
        movie.MovieId = id;
        return id;
    }

    public virtual bool Update(Movie movie)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Movie
            SET Title = @title,
                [Year] = @year,
                PosterUrl = @posterUrl,
                Genre = @genre,
                AverageRating = @averageRating
            WHERE MovieId = @id", connection);

        cmd.Parameters.AddWithValue("@id", movie.MovieId);
        cmd.Parameters.AddWithValue("@title", movie.Title);
        cmd.Parameters.AddWithValue("@year", movie.Year);
        cmd.Parameters.AddWithValue("@posterUrl", movie.PosterUrl);
        cmd.Parameters.AddWithValue("@genre", movie.Genre);
        cmd.Parameters.AddWithValue("@averageRating", movie.AverageRating);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public virtual bool Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM Movie WHERE MovieId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }
}