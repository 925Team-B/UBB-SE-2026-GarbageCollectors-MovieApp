using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class UserStatsRepositoryUnitTests
    {
        private readonly UserStatsRepository repo;

        public UserStatsRepositoryUnitTests()
        {
            repo = new UserStatsRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var stats = new UserStats
            {
                User = null,
                TotalPoints = 100,
                WeeklyScore = 20
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(stats));
            Assert.Equal("UserStats.User is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullUser_ThrowsInvalidOperationException()
        {
            var stats = new UserStats
            {
                StatsId = 1,
                User = null,
                TotalPoints = 150,
                WeeklyScore = 30
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(stats));
            Assert.Equal("UserStats.User is required for update.", ex.Message);
        }
    }
}