#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class BetRepository : IBetRepository
{
    private readonly string _connectionString;

    public BetRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<Bet> GetAll()
    {
        var bets = new List<Bet>();

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT UserId, BattleId, MovieId, Amount
            FROM Bet", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            bets.Add(MapBet(reader));
        }

        return bets;
    }

    public Bet? GetById(int userId, int battleId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT UserId, BattleId, MovieId, Amount
            FROM Bet
            WHERE UserId = @userId AND BattleId = @battleId", connection);

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@battleId", battleId);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapBet(reader);
    }

    public bool Insert(Bet bet)
    {
        if (bet.User is null)
            throw new InvalidOperationException("Bet.User is required for insert.");
        if (bet.Battle is null)
            throw new InvalidOperationException("Bet.Battle is required for insert.");
        if (bet.Movie is null)
            throw new InvalidOperationException("Bet.Movie is required for insert.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Bet (UserId, BattleId, MovieId, Amount)
            VALUES (@userId, @battleId, @movieId, @amount)", connection);

        cmd.Parameters.AddWithValue("@userId", bet.User.UserId);
        cmd.Parameters.AddWithValue("@battleId", bet.Battle.BattleId);
        cmd.Parameters.AddWithValue("@movieId", bet.Movie.MovieId);
        cmd.Parameters.AddWithValue("@amount", bet.Amount);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Update(Bet bet)
    {
        if (bet.User is null)
            throw new InvalidOperationException("Bet.User is required for update.");
        if (bet.Battle is null)
            throw new InvalidOperationException("Bet.Battle is required for update.");
        if (bet.Movie is null)
            throw new InvalidOperationException("Bet.Movie is required for update.");

        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Bet
            SET MovieId = @movieId,
                Amount = @amount
            WHERE UserId = @userId AND BattleId = @battleId", connection);

        cmd.Parameters.AddWithValue("@userId", bet.User.UserId);
        cmd.Parameters.AddWithValue("@battleId", bet.Battle.BattleId);
        cmd.Parameters.AddWithValue("@movieId", bet.Movie.MovieId);
        cmd.Parameters.AddWithValue("@amount", bet.Amount);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool Delete(int userId, int battleId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            DELETE FROM Bet
            WHERE UserId = @userId AND BattleId = @battleId", connection);

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@battleId", battleId);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>Deletes all bets for a given battle (used when resetting demo).</summary>
    public void DeleteByBattleId(int battleId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM Bet WHERE BattleId = @battleId", connection);
        cmd.Parameters.AddWithValue("@battleId", battleId);
        connection.Open();
        cmd.ExecuteNonQuery();
    }

    private static Bet MapBet(SqlDataReader reader)
    {
        return new Bet
        {
            Amount = reader.GetInt32(reader.GetOrdinal("Amount")),
            User = new User { UserId = reader.GetInt32(reader.GetOrdinal("UserId")) },
            Battle = new Battle { BattleId = reader.GetInt32(reader.GetOrdinal("BattleId")) },
            Movie = new Movie { MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")) }
        };
    }
}
