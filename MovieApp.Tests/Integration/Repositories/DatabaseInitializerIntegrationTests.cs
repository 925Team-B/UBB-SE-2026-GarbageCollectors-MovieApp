using Microsoft.Data.SqlClient;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class DatabaseInitializerIntegrationTests
    {
        private const string ConnectionString =
        "Server=.\\SQLEXPRESS;Database=MovieAppTestDb;Trusted_Connection=True;TrustServerCertificate=True;";
        [Fact]
        public void EnsureCreatedAndSeeded_Called_CreatesTables()
        {
            var initializer = new DatabaseInitializer(ConnectionString);

            initializer.EnsureCreatedAndSeeded();

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            string sql = @"
                SELECT COUNT(*)
                FROM sys.tables
                WHERE name IN ('User', 'Movie', 'Review', 'Battle', 'Bet', 'Comment', 'Badge', 'UserBadge', 'UserStats')";

            using var cmd = new SqlCommand(sql, conn);
            int tableCount = (int)cmd.ExecuteScalar();

            Assert.Equal(9, tableCount);
        }
    }
}