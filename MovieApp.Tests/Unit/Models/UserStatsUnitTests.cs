using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class UserStatsUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var stats = new UserStats();

            Assert.Equal(0, stats.StatsId);
            Assert.Equal(0, stats.TotalPoints);
            Assert.Equal(0, stats.WeeklyScore);
            Assert.Null(stats.User);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var user = new User { UserId = 5 };

            var stats = new UserStats
            {
                StatsId = 1,
                TotalPoints = 1500,
                WeeklyScore = 300,
                User = user
            };

            Assert.Equal(1, stats.StatsId);
            Assert.Equal(1500, stats.TotalPoints);
            Assert.Equal(300, stats.WeeklyScore);
            Assert.Same(user, stats.User);
        }

        [Fact]
        public void TotalPoints_CanBeZero()
        {
            var stats = new UserStats { TotalPoints = 0 };
            Assert.Equal(0, stats.TotalPoints);
        }

        [Fact]
        public void WeeklyScore_CanBeZero()
        {
            var stats = new UserStats { WeeklyScore = 0 };
            Assert.Equal(0, stats.WeeklyScore);
        }

        [Fact]
        public void User_CanBeSetToNull()
        {
            var stats = new UserStats { User = new User { UserId = 1 } };
            stats.User = null;

            Assert.Null(stats.User);
        }
    }
}