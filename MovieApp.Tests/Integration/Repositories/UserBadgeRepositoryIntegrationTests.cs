using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class UserBadgeRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly UserBadgeRepository repo;
        private readonly BadgeRepository badgeRepo;

        public UserBadgeRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_UserBadge_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new UserBadgeRepository(connectionString);
            badgeRepo = new BadgeRepository(connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM UserBadge", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Badge", conn).ExecuteNonQuery();
        }

        private Badge CreateBadge(string name, int criteriaValue)
        {
            var badge = new Badge
            {
                Name = name,
                CriteriaValue = criteriaValue
            };

            int id = badgeRepo.Insert(badge);
            badge.BadgeId = id;
            return badge;
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
IF DB_ID('{databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidUserBadge_ReturnsTrue()
        {
            var badge = CreateBadge("Test Badge", 10);

            var userBadge = new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            };

            bool inserted = repo.Insert(userBadge);

            Assert.True(inserted);

            var result = repo.GetById(1, badge.BadgeId);
            Assert.NotNull(result);
            Assert.Equal(1, result!.User!.UserId);
            Assert.Equal(badge.BadgeId, result.Badge!.BadgeId);
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var badge = CreateBadge("Test Badge", 10);

            var userBadge = new UserBadge
            {
                User = null,
                Badge = badge
            };

            Assert.Throws<InvalidOperationException>(() => repo.Insert(userBadge));
        }

        [Fact]
        public void GetAll_WhenUserBadgesExist_ReturnsUserBadges()
        {
            var badge1 = CreateBadge("Badge 1", 5);
            var badge2 = CreateBadge("Badge 2", 15);

            repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge1
            });

            repo.Insert(new UserBadge
            {
                User = ExistingUser(2),
                Badge = badge2
            });

            var userBadges = repo.GetAll();

            Assert.Equal(2, userBadges.Count);
        }

        [Fact]
        public void GetById_ExistingUserBadge_ReturnsUserBadge()
        {
            var badge = CreateBadge("Find Badge", 20);

            repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            });

            var result = repo.GetById(1, badge.BadgeId);

            Assert.NotNull(result);
            Assert.Equal(1, result!.User!.UserId);
            Assert.Equal(badge.BadgeId, result.Badge!.BadgeId);
        }

        [Fact]
        public void GetById_NonExistingUserBadge_ReturnsNull()
        {
            var result = repo.GetById(999, 999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingUserBadge_ReturnsTrue()
        {
            var badge = CreateBadge("Update Badge", 30);

            var userBadge = new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            };

            repo.Insert(userBadge);

            bool updated = repo.Update(userBadge);

            Assert.True(updated);
        }

        [Fact]
        public void Update_NonExistingUserBadge_ReturnsFalse()
        {
            var badge = CreateBadge("Ghost Badge", 40);

            var userBadge = new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            };

            bool updated = repo.Update(userBadge);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingUserBadge_ReturnsTrueAndRemovesUserBadge()
        {
            var badge = CreateBadge("Delete Badge", 50);

            repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            });

            bool deleted = repo.Delete(1, badge.BadgeId);

            Assert.True(deleted);
            Assert.Null(repo.GetById(1, badge.BadgeId));
        }

        [Fact]
        public void Delete_NonExistingUserBadge_ReturnsFalse()
        {
            bool deleted = repo.Delete(999, 999);

            Assert.False(deleted);
        }
    }
}