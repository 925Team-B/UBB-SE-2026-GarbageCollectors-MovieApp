#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class BadgeRepository
{
    private readonly string _connectionString;

    public BadgeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<Badge> GetAll()
    {
        var badges = new List<Badge>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT BadgeId, Name, CriteriaValue FROM Badge", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            badges.Add(new Badge
            {
                BadgeId = reader.GetInt32(reader.GetOrdinal("BadgeId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                CriteriaValue = reader.GetInt32(reader.GetOrdinal("CriteriaValue"))
            });
        }

        return badges;
    }

    public Badge? GetById(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT BadgeId, Name, CriteriaValue FROM Badge WHERE BadgeId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Badge
        {
            BadgeId = reader.GetInt32(reader.GetOrdinal("BadgeId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CriteriaValue = reader.GetInt32(reader.GetOrdinal("CriteriaValue"))
        };
    }

    public int Insert(Badge badge)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Badge (Name, CriteriaValue)
            VALUES (@name, @criteriaValue);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@name", badge.Name);
        cmd.Parameters.AddWithValue("@criteriaValue", badge.CriteriaValue);

        connection.Open();
        var id = (int)cmd.ExecuteScalar()!;
        badge.BadgeId = id;
        return id;
    }

    public bool Update(Badge badge)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Badge
            SET Name = @name,
                CriteriaValue = @criteriaValue
            WHERE BadgeId = @id", connection);

        cmd.Parameters.AddWithValue("@id", badge.BadgeId);
        cmd.Parameters.AddWithValue("@name", badge.Name);
        cmd.Parameters.AddWithValue("@criteriaValue", badge.CriteriaValue);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM Badge WHERE BadgeId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }
}
