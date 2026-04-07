using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class BadgeRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly BadgeRepository repo;

        public BadgeRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_Badge_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new BadgeRepository(connectionString);
            ClearBadgeTable();
        }

        private void ClearBadgeTable()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM UserBadge", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Badge", conn).ExecuteNonQuery();
        }

        public void Dispose()
        {
            var masterConnectionString =
                "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidBadge_ReturnsNewId()
        {
            var badge = new Badge
            {
                Name = "Test Badge",
                CriteriaValue = 10
            };

            int id = repo.Insert(badge);

            Assert.True(id > 0);

            var insertedBadge = repo.GetById(id);
            Assert.NotNull(insertedBadge);
            Assert.Equal("Test Badge", insertedBadge!.Name);
            Assert.Equal(10, insertedBadge.CriteriaValue);
        }

        [Fact]
        public void GetAll_WhenBadgesExist_ReturnsBadges()
        {
            repo.Insert(new Badge
            {
                Name = "Badge 1",
                CriteriaValue = 5
            });

            repo.Insert(new Badge
            {
                Name = "Badge 2",
                CriteriaValue = 15
            });

            var badges = repo.GetAll();

            Assert.Equal(2, badges.Count);
        }

        [Fact]
        public void GetById_ExistingBadge_ReturnsBadge()
        {
            var badge = new Badge
            {
                Name = "Find Me",
                CriteriaValue = 20
            };

            int id = repo.Insert(badge);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.BadgeId);
            Assert.Equal("Find Me", result.Name);
            Assert.Equal(20, result.CriteriaValue);
        }

        [Fact]
        public void GetById_NonExistingBadge_ReturnsNull()
        {
            var result = repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingBadge_ReturnsTrueAndUpdatesBadge()
        {
            var badge = new Badge
            {
                Name = "Old Badge",
                CriteriaValue = 25
            };

            int id = repo.Insert(badge);

            badge.BadgeId = id;
            badge.Name = "New Badge";
            badge.CriteriaValue = 50;

            bool updated = repo.Update(badge);

            Assert.True(updated);

            var updatedBadge = repo.GetById(id);
            Assert.NotNull(updatedBadge);
            Assert.Equal("New Badge", updatedBadge!.Name);
            Assert.Equal(50, updatedBadge.CriteriaValue);
        }

        [Fact]
        public void Update_NonExistingBadge_ReturnsFalse()
        {
            var badge = new Badge
            {
                BadgeId = 999999,
                Name = "Ghost Badge",
                CriteriaValue = 99
            };

            bool updated = repo.Update(badge);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingBadge_ReturnsTrueAndRemovesBadge()
        {
            var badge = new Badge
            {
                Name = "Delete Badge",
                CriteriaValue = 30
            };

            int id = repo.Insert(badge);

            bool deleted = repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingBadge_ReturnsFalse()
        {
            bool deleted = repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}