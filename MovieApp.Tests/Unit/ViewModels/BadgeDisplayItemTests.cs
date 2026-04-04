using MovieApp.Core.Models;
using MovieApp.UI.ViewModels;
using Xunit;

namespace Tests.Unit.ViewModels;

public class BadgeDisplayItemTests
{
    private static Badge MakeBadge(string name, int criteriaValue = 10) =>
        new Badge { Name = name, CriteriaValue = criteriaValue };


    [Fact]
    public void Badge_IsSetFromConstructor()
    {
        var badge = MakeBadge("The Godfather I");
        var item = new BadgeDisplayItem(badge, isUnlocked: false);

        Assert.Equal(badge, item.Badge);
    }

    [Fact]
    public void IsUnlocked_IsSetFromConstructor()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather I"), isUnlocked: true);

        Assert.True(item.IsUnlocked);
    }

    [Fact]
    public void Name_DelegatesToBadge()
    {
        var badge = MakeBadge("The Godfather I");
        var item = new BadgeDisplayItem(badge, isUnlocked: false);

        Assert.Equal("The Godfather I", item.Name);
    }

    [Fact]
    public void CriteriaValue_DelegatesToBadge()
    {
        var badge = MakeBadge("The Godfather I", criteriaValue: 42);
        var item = new BadgeDisplayItem(badge, isUnlocked: false);

        Assert.Equal(42, item.CriteriaValue);
    }


    [Fact]
    public void Icon_IsMedalEmoji_WhenUnlocked()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather I"), isUnlocked: true);

        Assert.Equal("🏅", item.Icon);
    }

    [Fact]
    public void Icon_IsPadlockEmoji_WhenLocked()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather I"), isUnlocked: false);

        Assert.Equal("🔒", item.Icon);
    }


    [Fact]
    public void CriteriaDescription_TheSnob_MentionsExtraReviews()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Snob", 5), isUnlocked: false);

        Assert.Equal("Write 5 extra reviews", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_WhySoSerious_MentionsFullyCompleteExtraReviews()
    {
        var item = new BadgeDisplayItem(MakeBadge("Why so serious?", 3), isUnlocked: false);

        Assert.Equal("Fully complete 3 extra reviews", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_TheJoker_MentionsComedyPercentage()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Joker", 99), isUnlocked: false);

        Assert.Equal("70%+ of your reviews on Comedy movies", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_TheGodfatherI_MentionsTotalReviews()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather I", 10), isUnlocked: false);

        Assert.Equal("Write 10 total reviews", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_TheGodfatherII_MentionsTotalReviews()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather II", 25), isUnlocked: false);

        Assert.Equal("Write 25 total reviews", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_TheGodfatherIII_MentionsTotalReviews()
    {
        var item = new BadgeDisplayItem(MakeBadge("The Godfather III", 50), isUnlocked: false);

        Assert.Equal("Write 50 total reviews", item.CriteriaDescription);
    }

    [Fact]
    public void CriteriaDescription_UnknownBadge_FallsBackToCriteriaValue()
    {
        var item = new BadgeDisplayItem(MakeBadge("Some Future Badge", 7), isUnlocked: false);

        Assert.Equal("Criteria value: 7", item.CriteriaDescription);
    }
}
