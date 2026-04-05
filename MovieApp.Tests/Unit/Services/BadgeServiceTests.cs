#nullable enable
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class BadgeServiceTests
{
    private readonly Mock<IUserBadgeRepository> _userBadgeRepoMock;
    private readonly Mock<IBadgeRepository> _badgeRepoMock;
    private readonly Mock<IReviewRepository> _reviewRepoMock;
    private readonly Mock<IMovieRepository> _movieRepoMock;
    private readonly BadgeService _sut;

    public BadgeServiceTests()
    {
        _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
        _badgeRepoMock = new Mock<IBadgeRepository>();
        _reviewRepoMock = new Mock<IReviewRepository>();
        _movieRepoMock = new Mock<IMovieRepository>();

        _sut = new BadgeService(
            _userBadgeRepoMock.Object,
            _badgeRepoMock.Object,
            _reviewRepoMock.Object,
            _movieRepoMock.Object);
    }

    // --- GetUserBadges ---

    [Fact]
    public async Task GetUserBadges_WhenUserHasBadges_ReturnsBadgesForThatUser()
    {
        var badge1 = new Badge { BadgeId = 1, Name = "The Snob", CriteriaValue = 10 };
        var badge2 = new Badge { BadgeId = 2, Name = "The Joker", CriteriaValue = 5 };
        var userBadges = new List<UserBadge>
        {
            new() { User = new User { UserId = 1 }, Badge = badge1 },
            new() { User = new User { UserId = 1 }, Badge = badge2 },
            new() { User = new User { UserId = 2 }, Badge = badge1 }
        };
        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(userBadges);

        var result = await _sut.GetUserBadges(1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.BadgeId == 1);
        Assert.Contains(result, b => b.BadgeId == 2);
    }

    [Fact]
    public async Task GetUserBadges_WhenUserHasNoBadges_ReturnsEmptyList()
    {
        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());

        var result = await _sut.GetUserBadges(99);

        Assert.Empty(result);
    }

    // --- GetAllBadges ---

    [Fact]
    public async Task GetAllBadges_WhenBadgesExist_ReturnsAllBadges()
    {
        var badges = new List<Badge>
        {
            new() { BadgeId = 1, Name = "The Snob" },
            new() { BadgeId = 2, Name = "The Joker" }
        };
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(badges);

        var result = await _sut.GetAllBadges();

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

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { snobBadge });
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        _movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == snobBadge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserAlreadyHasBadge_DoesNotInsertDuplicate()
    {
        const int userId = 1;
        var snobBadge = new Badge { BadgeId = 1, Name = "The Snob" };
        var existingUserBadge = new UserBadge { User = new User { UserId = userId }, Badge = snobBadge };

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge> { existingUserBadge });
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { snobBadge });
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(Enumerable.Range(1, 15)
            .Select(i => new Review { User = new User { UserId = userId }, Movie = new Movie { MovieId = i, Genre = "Drama" }, IsExtraReview = true })
            .ToList());
        _movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie>());

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.IsAny<UserBadge>()), Times.Never);
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
            new() { User = user, Movie = comedyMovie, IsExtraReview = false },
            new() { User = user, Movie = comedyMovie, IsExtraReview = false },
            new() { User = user, Movie = comedyMovie, IsExtraReview = false },
            new() { User = user, Movie = dramaMovie, IsExtraReview = false }
        };

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { jokerBadge });
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        _movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie> { comedyMovie, dramaMovie });

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
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

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { godfatherBadge });
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        _movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
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

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(new List<Badge> { badge });
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(reviews);
        _movieRepoMock.Setup(r => r.GetAll()).Returns(reviews.Select(r => r.Movie!).ToList());

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.Is<UserBadge>(ub =>
            ub.Badge != null && ub.Badge.BadgeId == badge.BadgeId)), Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadges_WhenUserHasNoReviews_AwardsNoBadges()
    {
        const int userId = 1;
        var badges = new List<Badge>
        {
            new() { BadgeId = 1, Name = "The Snob" },
            new() { BadgeId = 2, Name = "The Joker" }
        };

        _userBadgeRepoMock.Setup(r => r.GetAll()).Returns(new List<UserBadge>());
        _badgeRepoMock.Setup(r => r.GetAll()).Returns(badges);
        _reviewRepoMock.Setup(r => r.GetAll()).Returns(new List<Review>());
        _movieRepoMock.Setup(r => r.GetAll()).Returns(new List<Movie>());

        await _sut.CheckAndAwardBadges(userId);

        _userBadgeRepoMock.Verify(r => r.Insert(It.IsAny<UserBadge>()), Times.Never);
    }
}
