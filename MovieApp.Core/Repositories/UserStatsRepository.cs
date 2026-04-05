#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class UserStatsRepository
{
    private readonly string _connectionString;

    public UserStatsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual List<UserStats> GetAll()
    {
        var stats = new List<UserStats>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT StatsId, UserId, TotalPoints, WeeklyScore
            FROM UserStats", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            stats.Add(MapUserStats(reader));
        }

        return stats;
    }

    public virtual UserStats? GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT StatsId, UserId, TotalPoints, WeeklyScore
            FROM UserStats
            WHERE StatsId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapUserStats(reader);
    }

    public virtual UserStats? GetByUserId(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT StatsId, UserId, TotalPoints, WeeklyScore
            FROM UserStats
            WHERE UserId = @userId", connection);

        cmd.Parameters.AddWithValue("@userId", userId);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapUserStats(reader);
    }

    public virtual int Insert(UserStats stats)
    {
        if (stats.User is null)
            throw new InvalidOperationException("UserStats.User is required for insert.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO UserStats (UserId, TotalPoints, WeeklyScore)
            VALUES (@userId, @totalPoints, @weeklyScore);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@userId", stats.User.UserId);
        cmd.Parameters.AddWithValue("@totalPoints", stats.TotalPoints);
        cmd.Parameters.AddWithValue("@weeklyScore", stats.WeeklyScore);

        connection.Open();
        var id = (int)cmd.ExecuteScalar()!;
        stats.StatsId = id;
        return id;
    }

    public virtual bool Update(UserStats stats)
    {
        if (stats.User is null)
            throw new InvalidOperationException("UserStats.User is required for update.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE UserStats
            SET UserId = @userId,
                TotalPoints = @totalPoints,
                WeeklyScore = @weeklyScore
            WHERE StatsId = @id", connection);

        cmd.Parameters.AddWithValue("@id", stats.StatsId);
        cmd.Parameters.AddWithValue("@userId", stats.User.UserId);
        cmd.Parameters.AddWithValue("@totalPoints", stats.TotalPoints);
        cmd.Parameters.AddWithValue("@weeklyScore", stats.WeeklyScore);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public virtual bool Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM UserStats WHERE StatsId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    private static UserStats MapUserStats(SqlDataReader reader)
    {
        return new UserStats
        {
            StatsId = reader.GetInt32(reader.GetOrdinal("StatsId")),
            TotalPoints = reader.GetInt32(reader.GetOrdinal("TotalPoints")),
            WeeklyScore = reader.GetInt32(reader.GetOrdinal("WeeklyScore")),
            User = new User { UserId = reader.GetInt32(reader.GetOrdinal("UserId")) }
        };
    }
}