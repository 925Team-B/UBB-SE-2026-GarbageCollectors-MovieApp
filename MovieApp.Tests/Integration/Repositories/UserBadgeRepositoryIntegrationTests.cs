using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class UserBadgeRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly UserBadgeRepository _repo;
        private readonly BadgeRepository _badgeRepo;

        public UserBadgeRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_UserBadge_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new UserBadgeRepository(_connectionString);
            _badgeRepo = new BadgeRepository(_connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(_connectionString);
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

            int id = _badgeRepo.Insert(badge);
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
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
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

            bool inserted = _repo.Insert(userBadge);

            Assert.True(inserted);

            var result = _repo.GetById(1, badge.BadgeId);
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

            Assert.Throws<InvalidOperationException>(() => _repo.Insert(userBadge));
        }

        [Fact]
        public void GetAll_WhenUserBadgesExist_ReturnsUserBadges()
        {
            var badge1 = CreateBadge("Badge 1", 5);
            var badge2 = CreateBadge("Badge 2", 15);

            _repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge1
            });

            _repo.Insert(new UserBadge
            {
                User = ExistingUser(2),
                Badge = badge2
            });

            var userBadges = _repo.GetAll();

            Assert.Equal(2, userBadges.Count);
        }

        [Fact]
        public void GetById_ExistingUserBadge_ReturnsUserBadge()
        {
            var badge = CreateBadge("Find Badge", 20);

            _repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            });

            var result = _repo.GetById(1, badge.BadgeId);

            Assert.NotNull(result);
            Assert.Equal(1, result!.User!.UserId);
            Assert.Equal(badge.BadgeId, result.Badge!.BadgeId);
        }

        [Fact]
        public void GetById_NonExistingUserBadge_ReturnsNull()
        {
            var result = _repo.GetById(999, 999);

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

            _repo.Insert(userBadge);

            bool updated = _repo.Update(userBadge);

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

            bool updated = _repo.Update(userBadge);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingUserBadge_ReturnsTrueAndRemovesUserBadge()
        {
            var badge = CreateBadge("Delete Badge", 50);

            _repo.Insert(new UserBadge
            {
                User = ExistingUser(1),
                Badge = badge
            });

            bool deleted = _repo.Delete(1, badge.BadgeId);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(1, badge.BadgeId));
        }

        [Fact]
        public void Delete_NonExistingUserBadge_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999, 999);

            Assert.False(deleted);
        }
    }
}