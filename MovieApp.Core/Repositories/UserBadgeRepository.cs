#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class UserBadgeRepository
{
    private readonly string _connectionString;

    public UserBadgeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<UserBadge> GetAll()
    {
        var userBadges = new List<UserBadge>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT UserId, BadgeId
            FROM UserBadge", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            userBadges.Add(MapUserBadge(reader));
        }

        return userBadges;
    }

    public UserBadge? GetById(int userId, int badgeId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT UserId, BadgeId
            FROM UserBadge
            WHERE UserId = @userId AND BadgeId = @badgeId", connection);

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@badgeId", badgeId);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapUserBadge(reader);
    }

    public bool Insert(UserBadge userBadge)
    {
        if (userBadge.User is null)
            throw new InvalidOperationException("UserBadge.User is required for insert.");
        if (userBadge.Badge is null)
            throw new InvalidOperationException("UserBadge.Badge is required for insert.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO UserBadge (UserId, BadgeId)
            VALUES (@userId, @badgeId)", connection);

        cmd.Parameters.AddWithValue("@userId", userBadge.User.UserId);
        cmd.Parameters.AddWithValue("@badgeId", userBadge.Badge.BadgeId);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Update(UserBadge userBadge)
    {
        if (userBadge.User is null)
            throw new InvalidOperationException("UserBadge.User is required for update.");
        if (userBadge.Badge is null)
            throw new InvalidOperationException("UserBadge.Badge is required for update.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE UserBadge
            SET UserId = @userId,
                BadgeId = @badgeId
            WHERE UserId = @userId AND BadgeId = @badgeId", connection);

        cmd.Parameters.AddWithValue("@userId", userBadge.User.UserId);
        cmd.Parameters.AddWithValue("@badgeId", userBadge.Badge.BadgeId);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int userId, int badgeId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            DELETE FROM UserBadge
            WHERE UserId = @userId AND BadgeId = @badgeId", connection);

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@badgeId", badgeId);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    private static UserBadge MapUserBadge(SqlDataReader reader)
    {
        return new UserBadge
        {
            User = new User { UserId = reader.GetInt32(reader.GetOrdinal("UserId")) },
            Badge = new Badge { BadgeId = reader.GetInt32(reader.GetOrdinal("BadgeId")) }
        };
    }
}
