#nullable enable
using Moq;
using MovieApp.Core.Interfaces.Repository;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class BadgeServiceTests
{
    private readonly Mock<IUserBadgeRepository> userBadgeRepoMock;
    private readonly Mock<IBadgeRepository> badgeRepoMock;
    private readonly Mock<IReviewRepository> reviewRepoMock;
    private readonly Mock<IMovieRepository> movieRepoMock;
    private readonly BadgeService sut;

    public BadgeServiceTests()
    {
        userBadgeRepoMock = new Mock<IUserBadgeRepository>();
        badgeRepoMock = new Mock<IBadgeRepository>();
        reviewRepoMock = new Mock<IReviewRepository>();
        movieRepoMock = new Mock<IMovieRepository>();

        sut = new BadgeService(
            userBadgeRepoMock.Object,
            badgeRepoMock.Object,
            reviewRepoMock.Object,
            movieRepoMock.Object);
    }

    // --- GetUserBadges ---
    [Fact]
    public async Task GetUserBadges_WhenUserHasBadges_ReturnsBadgesForThatUser()
    {
        var badge1 = new Badge { BadgeId = 1, Name = "The Snob", CriteriaValue = 10 };
        var badge2 = new Badge { BadgeId = 2, Name = "The Joker", CriteriaValue = 5 };
        var userBadges = new List<UserBadge>
        {
            new () { User = new User { UserId = 1 }, Badge = badge1 },
            new () { User = new User { UserId = 1 }, Badge = badge2 },
            new () { User = new User { UserId = 2 }, Badge = badge1 }
        };
        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(userBadges);

        var result = await sut.GetUserBadges(1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.BadgeId == 1);
        Assert.Contains(result, b => b.BadgeId == 2);
    }

    [Fact]
    public async Task GetUserBadges_WhenUserHasNoBadges_ReturnsEmptyList()
    {
        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());

        var result = await sut.GetUserBadges(99);

        Assert.Empty(result);
    }

    // --- GetAllBadges ---
    [Fact]
    public async Task GetAllBadges_WhenBadgesExist_ReturnsAllBadges()
    {
        var badges = new List<Badge>
        {
            new () { BadgeId = 1, Name = "The Snob" },
            new () { BadgeId = 2, Name = "The Joker" }
        };
        badgeRepoMock.Setup(r => r.GetAll()).Returns(badges);

        var result = await sut.GetAllBadges();

        Assert.Equal(2, result.Count);
    }

    // --- CheckAndAwardBadges ---
    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHas10ExtraReviews_AwardsTheSnobBadge()
    {
        const int userId = 1;
        var snobBadge = new Badge { BadgeId = 1, Name = "The Snob", CriteriaValue = 10 };
        var user = new User { UserId = userId };
        var reviews = Enumerable.Range(1, 10)
            .Select(i => new Review { User = user, Movie = new Movie { MovieId = i, Genre = "Drama" }, IsExtraReview = true })
            .ToList();

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { snobBadge });
        reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == snobBadge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserAlreadyHasBadge_DoesNotInsertDuplicate()
    {
        const int userId = 1;
        var snobBadge = new Badge { BadgeId = 1, Name = "The Snob" };
        var existingUserBadge = new UserBadge { User = new User { UserId = userId }, Badge = snobBadge };

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge> { existingUserBadge });
        badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { snobBadge });
        reviewRepoMock.Setup(r => r.GetAll()).Returns(Enumerable.Range(1, 15)
            .Select(i => new Review { User = new User { UserId = userId }, Movie = new Movie { MovieId = i, Genre = "Drama" }, IsExtraReview = true })
            .ToList());
        movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie>());

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.IsAny<UserBadge>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHasMoreThan70PercentComedyReviews_AwardsTheJokerBadge()
    {
        const int userId = 1;
        var jokerBadge = new Badge { BadgeId = 2, Name = "The Joker" };
        var user = new User { UserId = userId };

        var comedyMovie = new Movie { MovieId = 1, Genre = "Comedy" };
        var dramaMovie = new Movie { MovieId = 2, Genre = "Drama" };

        var reviews = new List<Review>
        {
            new () { User = user, Movie = comedyMovie, IsExtraReview = false },
            new () { User = user, Movie = comedyMovie, IsExtraReview = false },
            new () { User = user, Movie = comedyMovie, IsExtraReview = false },
            new () { User = user, Movie = dramaMovie, IsExtraReview = false }
        };

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { jokerBadge });
        reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie> { comedyMovie, dramaMovie });

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == jokerBadge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHas100TotalReviews_AwardsGodfatherIBadge()
    {
        const int userId = 1;
        var godfatherBadge = new Badge { BadgeId = 3, Name = "The Godfather I" };
        var user = new User { UserId = userId };
        var reviews = Enumerable.Range(1, 100)
            .Select(i => new Review { User = user, Movie = new Movie { MovieId = i, Genre = "Drama" }, IsExtraReview = false })
            .ToList();

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { godfatherBadge });
        reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == godfatherBadge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHas50FullyCompletedExtraReviews_AwardsWhySoSeriousBadge()
    {
        const int userId = 1;
        var badge = new Badge { BadgeId = 4, Name = "Why so serious?" };
        var user = new User { UserId = userId };
        var reviews = Enumerable.Range(1, 50).Select(i => new Review
        {
            User = user,
            Movie = new Movie { MovieId = i, Genre = "Drama" },
            IsExtraReview = true,
            CinematographyText = "text",
            ActingText = "text",
            CgiText = "text",
            PlotText = "text",
            SoundText = "text"
        }).ToList();

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { badge });
        reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == badge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHasNoReviews_AwardsNoBadges()
    {
        const int userId = 1;
        var badges = new List<Badge>
        {
            new () { BadgeId = 1, Name = "The Snob" },
            new () { BadgeId = 2, Name = "The Joker" }
        };

        userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        badgeRepoMock.Setup(r => r.GetAll()).Returns(badges);
        reviewRepoMock.Setup(r => r.GetAll()).Returns(new List<Review>());
        movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie>());

        await sut.CheckAndAwardBadges(userId);

        userBadgeRepoMock.Verify(r => r.Insert(It.IsAny<UserBadge>()), Times.Never);
    }
}
