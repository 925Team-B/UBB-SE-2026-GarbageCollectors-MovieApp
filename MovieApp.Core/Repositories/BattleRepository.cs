#nullable enable
using Microsoft.Data.SqlClient;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;

namespace MovieApp.Core.Repositories;

public class BattleRepository : IBattleRepository
{
    private readonly string connectionString;

    public BattleRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public virtual List<Battle> GetAll()
    {
        var battles = new List<Battle>();

        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            SELECT BattleId, FirstMovieId, SecondMovieId, InitialRatingFirstMovie,
                   InitialRatingSecondMovie, StartDate, EndDate, Status
            FROM Battle", connection);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            battles.Add(MapBattle(reader));
        }

        return battles;
    }

    public virtual Battle? GetById(int id)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            SELECT BattleId, FirstMovieId, SecondMovieId, InitialRatingFirstMovie,
                   InitialRatingSecondMovie, StartDate, EndDate, Status
            FROM Battle
            WHERE BattleId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return MapBattle(reader);
    }

    public virtual int Insert(Battle battle)
    {
        if (battle.FirstMovie is null)
        {
            throw new InvalidOperationException("Battle.FirstMovie is required for insert.");
        }
        if (battle.SecondMovie is null)
        {
            throw new InvalidOperationException("Battle.SecondMovie is required for insert.");
        }

        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            INSERT INTO Battle (FirstMovieId, SecondMovieId, InitialRatingFirstMovie,
                                 InitialRatingSecondMovie, StartDate, EndDate, Status)
            VALUES (@firstMovieId, @secondMovieId, @initialRatingFirstMovie,
                    @initialRatingSecondMovie, @startDate, @endDate, @status);
            SELECT CAST(SCOPE_IDENTITY() AS int);", connection);

        cmd.Parameters.AddWithValue("@firstMovieId", battle.FirstMovie.MovieId);
        cmd.Parameters.AddWithValue("@secondMovieId", battle.SecondMovie.MovieId);
        cmd.Parameters.AddWithValue("@initialRatingFirstMovie", battle.InitialRatingFirstMovie);
        cmd.Parameters.AddWithValue("@initialRatingSecondMovie", battle.InitialRatingSecondMovie);
        cmd.Parameters.AddWithValue("@startDate", battle.StartDate);
        cmd.Parameters.AddWithValue("@endDate", battle.EndDate);
        cmd.Parameters.AddWithValue("@status", StatusToInt(battle.Status));

        connection.Open();
        var id = (int)cmd.ExecuteScalar() !;
        battle.BattleId = id;
        return id;
    }

    public virtual bool Update(Battle battle)
    {
        if (battle.FirstMovie is null)
        {
            throw new InvalidOperationException("Battle.FirstMovie is required for update.");
        }
        if (battle.SecondMovie is null)
        {
            throw new InvalidOperationException("Battle.SecondMovie is required for update.");
        }

        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(@"
            UPDATE Battle
            SET FirstMovieId = @firstMovieId,
                SecondMovieId = @secondMovieId,
                InitialRatingFirstMovie = @initialRatingFirstMovie,
                InitialRatingSecondMovie = @initialRatingSecondMovie,
                StartDate = @startDate,
                EndDate = @endDate,
                Status = @status
            WHERE BattleId = @id", connection);

        cmd.Parameters.AddWithValue("@id", battle.BattleId);
        cmd.Parameters.AddWithValue("@firstMovieId", battle.FirstMovie.MovieId);
        cmd.Parameters.AddWithValue("@secondMovieId", battle.SecondMovie.MovieId);
        cmd.Parameters.AddWithValue("@initialRatingFirstMovie", battle.InitialRatingFirstMovie);
        cmd.Parameters.AddWithValue("@initialRatingSecondMovie", battle.InitialRatingSecondMovie);
        cmd.Parameters.AddWithValue("@startDate", battle.StartDate);
        cmd.Parameters.AddWithValue("@endDate", battle.EndDate);
        cmd.Parameters.AddWithValue("@status", StatusToInt(battle.Status));

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    public virtual bool Delete(int id)
    {
        using var connection = new SqlConnection(connectionString);
        using var cmd = new SqlCommand("DELETE FROM Battle WHERE BattleId = @id", connection);

        cmd.Parameters.AddWithValue("@id", id);

        connection.Open();
        return cmd.ExecuteNonQuery() > 0;
    }

    private static Battle MapBattle(SqlDataReader reader)
    {
        return new Battle
        {
            BattleId = reader.GetInt32(reader.GetOrdinal("BattleId")),
            InitialRatingFirstMovie = reader.GetDouble(reader.GetOrdinal("InitialRatingFirstMovie")),
            InitialRatingSecondMovie = reader.GetDouble(reader.GetOrdinal("InitialRatingSecondMovie")),
            StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
            EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
            Status = StatusFromInt(reader.GetInt32(reader.GetOrdinal("Status"))),
            FirstMovie = new Movie { MovieId = reader.GetInt32(reader.GetOrdinal("FirstMovieId")) },
            SecondMovie = new Movie { MovieId = reader.GetInt32(reader.GetOrdinal("SecondMovieId")) }
        };
    }

    private static string StatusFromInt(int status) => status switch
    {
        1 => "Active",
        2 => "Finished",
        _ => "Pending"
    };

    private static int StatusToInt(string status) => status switch
    {
        "Active" => 1,
        "Finished" => 2,
        _ => 0
    };
}