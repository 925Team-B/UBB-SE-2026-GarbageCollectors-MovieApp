using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class UserStatsRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly UserStatsRepository _repo;

        public UserStatsRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_UserStats_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new UserStatsRepository(_connectionString);
            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM UserStats", conn).ExecuteNonQuery();
        }

        private User ExistingUser(int userId = 1)
        {
            return new User { UserId = userId };
        }

        public void Dispose()
        {
            var masterConnectionString =
                "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidUserStats_ReturnsNewId()
        {
            var stats = new UserStats
            {
                User = ExistingUser(1),
                TotalPoints = 100,
                WeeklyScore = 25
            };

            int id = _repo.Insert(stats);

            Assert.True(id > 0);

            var insertedStats = _repo.GetById(id);
            Assert.NotNull(insertedStats);
            Assert.Equal(100, insertedStats!.TotalPoints);
            Assert.Equal(25, insertedStats.WeeklyScore);
            Assert.Equal(1, insertedStats.User!.UserId);
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var stats = new UserStats
            {
                User = null,
                TotalPoints = 100,
                WeeklyScore = 25
            };

            Assert.Throws<InvalidOperationException>(() => _repo.Insert(stats));
        }

        [Fact]
        public void GetAll_WhenStatsExist_ReturnsStats()
        {
            _repo.Insert(new UserStats
            {
                User = ExistingUser(1),
                TotalPoints = 10,
                WeeklyScore = 2
            });

            _repo.Insert(new UserStats
            {
                User = ExistingUser(2),
                TotalPoints = 20,
                WeeklyScore = 5
            });

            var allStats = _repo.GetAll();

            Assert.Equal(2, allStats.Count);
        }

        [Fact]
        public void GetById_ExistingStats_ReturnsStats()
        {
            var stats = new UserStats
            {
                User = ExistingUser(1),
                TotalPoints = 50,
                WeeklyScore = 10
            };

            int id = _repo.Insert(stats);

            var result = _repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.StatsId);
            Assert.Equal(50, result.TotalPoints);
            Assert.Equal(10, result.WeeklyScore);
            Assert.Equal(1, result.User!.UserId);
        }

        [Fact]
        public void GetById_NonExistingStats_ReturnsNull()
        {
            var result = _repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void GetByUserId_ExistingUser_ReturnsStats()
        {
            _repo.Insert(new UserStats
            {
                User = ExistingUser(2),
                TotalPoints = 70,
                WeeklyScore = 12
            });

            var result = _repo.GetByUserId(2);

            Assert.NotNull(result);
            Assert.Equal(70, result!.TotalPoints);
            Assert.Equal(12, result.WeeklyScore);
            Assert.Equal(2, result.User!.UserId);
        }

        [Fact]
        public void GetByUserId_NonExistingUser_ReturnsNull()
        {
            var result = _repo.GetByUserId(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingStats_ReturnsTrueAndUpdatesStats()
        {
            var stats = new UserStats
            {
                User = ExistingUser(1),
                TotalPoints = 40,
                WeeklyScore = 8
            };

            int id = _repo.Insert(stats);

            stats.StatsId = id;
            stats.User = ExistingUser(2);
            stats.TotalPoints = 90;
            stats.WeeklyScore = 30;

            bool updated = _repo.Update(stats);

            Assert.True(updated);

            var updatedStats = _repo.GetById(id);
            Assert.NotNull(updatedStats);
            Assert.Equal(90, updatedStats!.TotalPoints);
            Assert.Equal(30, updatedStats.WeeklyScore);
            Assert.Equal(2, updatedStats.User!.UserId);
        }

        [Fact]
        public void Update_NonExistingStats_ReturnsFalse()
        {
            var stats = new UserStats
            {
                StatsId = 999999,
                User = ExistingUser(1),
                TotalPoints = 40,
                WeeklyScore = 8
            };

            bool updated = _repo.Update(stats);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingStats_ReturnsTrueAndRemovesStats()
        {
            var stats = new UserStats
            {
                User = ExistingUser(1),
                TotalPoints = 60,
                WeeklyScore = 15
            };

            int id = _repo.Insert(stats);

            bool deleted = _repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingStats_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}