#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string connectionString;

    public UserRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public virtual List<User> GetAll()
    {
        var users = new List<User>();

        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("SELECT UserId FROM [User]", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
            });
        }

        return users;
    }

    public virtual User? GetById(int id)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("SELECT UserId FROM [User] WHERE UserId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("UserId"))
        };
    }

    public virtual int Insert(User user)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO [User] DEFAULT VALUES;
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        connection.Open();
        var id = (int)cmd.ExecuteScalar() !;
        user.UserId = id;
        return id;
    }

    public virtual bool Update(User user)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE [User]
            SET UserId = UserId
            WHERE UserId = @id", connection);

        cmd.Parameters.AddWithValue("@id", user.UserId);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public virtual bool Delete(int id)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("DELETE FROM [User] WHERE UserId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }
}