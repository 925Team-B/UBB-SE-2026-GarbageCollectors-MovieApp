using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
	public class BadgeUnitTests
	{
		[Fact]
		public void DefaultConstructor_SetsExpectedDefaults()
		{
			var badge = new Badge();

			Assert.Equal(0, badge.BadgeId);
			Assert.Equal(string.Empty, badge.Name);
			Assert.Equal(0, badge.CriteriaValue);
			Assert.NotNull(badge.UserBadges);
			Assert.Empty(badge.UserBadges);
		}

		[Fact]
		public void Properties_CanBeSetAndRead()
		{
			var badge = new Badge
			{
				BadgeId = 42,
				Name = "Top Reviewer",
				CriteriaValue = 100
			};

			Assert.Equal(42, badge.BadgeId);
			Assert.Equal("Top Reviewer", badge.Name);
			Assert.Equal(100, badge.CriteriaValue);
		}

		[Fact]
		public void UserBadges_CanAddItems()
		{
			var badge = new Badge();
			badge.UserBadges.Add(new UserBadge());
			badge.UserBadges.Add(new UserBadge());

			Assert.Equal(2, badge.UserBadges.Count);
		}
	}
}