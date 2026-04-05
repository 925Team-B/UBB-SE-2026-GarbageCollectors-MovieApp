using Microsoft.Data.SqlClient;
using MovieApp.Core.Repositories;

namespace Tests.Integration.Services;

public abstract class IntegrationTestBase : IDisposable
{
    protected const string ConnectionString =
        @"Data Source=(localdb)\MovieAppTest;Initial Catalog=MovieAppIntegrationTests;Integrated Security=True;TrustServerCertificate=True;";

    protected IntegrationTestBase()
    {
        var initializer = new DatabaseInitializer(ConnectionString);
        initializer.EnsureCreatedAndSeeded();
        CleanTables();
    }

    private void CleanTables()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var cmd = new SqlCommand(@"
            DELETE FROM Bet;
            DELETE FROM Comment;
            DELETE FROM Battle;
            DELETE FROM Review;
            DELETE FROM UserBadge;
            DELETE FROM UserStats;
            DELETE FROM [User];
            DELETE FROM Movie;
            DELETE FROM Badge;
            DBCC CHECKIDENT ('UserStats', RESEED, 0);
            DBCC CHECKIDENT ('Review', RESEED, 0);
            DBCC CHECKIDENT ('[User]', RESEED, 0);
            DBCC CHECKIDENT ('Movie', RESEED, 0);
            DBCC CHECKIDENT ('Badge', RESEED, 0);
            DBCC CHECKIDENT ('Battle', RESEED, 0);
        ", connection);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => CleanTables();
}
