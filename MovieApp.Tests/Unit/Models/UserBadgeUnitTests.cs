using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class UserBadgeUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var userBadge = new UserBadge();

            Assert.Null(userBadge.User);
            Assert.Null(userBadge.Badge);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var user = new User { UserId = 1 };
            var badge = new Badge { BadgeId = 2, Name = "Cinephile" };

            var userBadge = new UserBadge
            {
                User = user,
                Badge = badge
            };

            Assert.Same(user, userBadge.User);
            Assert.Same(badge, userBadge.Badge);
        }

        [Fact]
        public void User_CanBeReassigned()
        {
            var original = new User { UserId = 1 };
            var replacement = new User { UserId = 2 };
            var userBadge = new UserBadge { User = original };

            userBadge.User = replacement;

            Assert.Same(replacement, userBadge.User);
        }

        [Fact]
        public void Badge_CanBeReassigned()
        {
            var original = new Badge { BadgeId = 1 };
            var replacement = new Badge { BadgeId = 2 };
            var userBadge = new UserBadge { Badge = original };

            userBadge.Badge = replacement;

            Assert.Same(replacement, userBadge.Badge);
        }
    }
}