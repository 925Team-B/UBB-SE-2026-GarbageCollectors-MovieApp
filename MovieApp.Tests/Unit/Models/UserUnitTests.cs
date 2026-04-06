using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class UserUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var user = new User();

            Assert.Equal(0, user.UserId);
            Assert.NotNull(user.Reviews);
            Assert.Empty(user.Reviews);
            Assert.NotNull(user.Comments);
            Assert.Empty(user.Comments);
            Assert.NotNull(user.Bets);
            Assert.Empty(user.Bets);
            Assert.Null(user.UserStats);
            Assert.NotNull(user.UserBadges);
            Assert.Empty(user.UserBadges);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var stats = new UserStats { StatsId = 1, TotalPoints = 200 };

            var user = new User
            {
                UserId = 7,
                UserStats = stats
            };

            Assert.Equal(7, user.UserId);
            Assert.Same(stats, user.UserStats);
        }

        [Fact]
        public void Reviews_CanAddItems()
        {
            var user = new User();
            user.Reviews.Add(new Review());
            user.Reviews.Add(new Review());

            Assert.Equal(2, user.Reviews.Count);
        }

        [Fact]
        public void Comments_CanAddItems()
        {
            var user = new User();
            user.Comments.Add(new Comment());

            Assert.Single(user.Comments);
        }

        [Fact]
        public void Bets_CanAddItems()
        {
            var user = new User();
            user.Bets.Add(new Bet { Amount = 10 });
            user.Bets.Add(new Bet { Amount = 20 });

            Assert.Equal(2, user.Bets.Count);
        }

        [Fact]
        public void UserBadges_CanAddItems()
        {
            var user = new User();
            user.UserBadges.Add(new UserBadge());

            Assert.Single(user.UserBadges);
        }
    }
}