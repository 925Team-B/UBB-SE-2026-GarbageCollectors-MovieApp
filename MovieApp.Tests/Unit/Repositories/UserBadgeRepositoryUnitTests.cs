using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class UserBadgeRepositoryUnitTests
    {
        private readonly UserBadgeRepository _repo;

        public UserBadgeRepositoryUnitTests()
        {
            _repo = new UserBadgeRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var userBadge = new UserBadge
            {
                User = null,
                Badge = new Badge { BadgeId = 1 }
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(userBadge));
            Assert.Equal("UserBadge.User is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullBadge_ThrowsInvalidOperationException()
        {
            var userBadge = new UserBadge
            {
                User = new User { UserId = 1 },
                Badge = null
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(userBadge));
            Assert.Equal("UserBadge.Badge is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullUser_ThrowsInvalidOperationException()
        {
            var userBadge = new UserBadge
            {
                User = null,
                Badge = new Badge { BadgeId = 1 }
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(userBadge));
            Assert.Equal("UserBadge.User is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullBadge_ThrowsInvalidOperationException()
        {
            var userBadge = new UserBadge
            {
                User = new User { UserId = 1 },
                Badge = null
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(userBadge));
            Assert.Equal("UserBadge.Badge is required for update.", ex.Message);
        }
    }
}