using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using MovieApp.Core.Services;

namespace Tests.Unit.Services;

internal class FakeUserStatsRepository : UserStatsRepository
{
    private readonly List<UserStats> _store = new();
    private int _nextId = 1;

    public FakeUserStatsRepository() : base(string.Empty) { }

    public override UserStats? GetByUserId(int userId) =>
        _store.FirstOrDefault(s => s.User?.UserId == userId);

    public override int Insert(UserStats stats)
    {
        stats.StatsId = _nextId++;
        _store.Add(stats);
        return stats.StatsId;
    }

    public override bool Update(UserStats stats)
    {
        var existing = _store.FirstOrDefault(s => s.StatsId == stats.StatsId);
        if (existing is null) return false;
        existing.TotalPoints = stats.TotalPoints;
        existing.WeeklyScore = stats.WeeklyScore;
        return true;
    }
}

internal class FakeUserRepository : UserRepository
{
    private readonly Dictionary<int, User> _store;

    public FakeUserRepository(params User[] users) : base(string.Empty)
    {
        _store = users.ToDictionary(u => u.UserId);
    }

    public override User? GetById(int id) =>
        _store.TryGetValue(id, out var u) ? u : null;
}

internal class FakeMovieRepository : MovieRepository
{
    private readonly Dictionary<int, Movie> _store;

    public FakeMovieRepository(params Movie[] movies) : base(string.Empty)
    {
        _store = movies.ToDictionary(m => m.MovieId);
    }

    public override Movie? GetById(int id) =>
        _store.TryGetValue(id, out var m) ? m : null;

    public override bool Update(Movie movie)
    {
        _store[movie.MovieId] = movie;
        return true;
    }
}

public class PointServiceTests
{
    private static User DefaultUser(int id = 1) => new() { UserId = id };

    private static Movie DefaultMovie(int id = 10, double avgRating = 3.0) =>
        new() { MovieId = id, AverageRating = avgRating };

    private PointService CreateSut(
        FakeUserStatsRepository? statsRepo = null,
        FakeUserRepository? userRepo = null,
        FakeMovieRepository? movieRepo = null,
        IBadgeService? badgeService = null)
    {
        statsRepo ??= new FakeUserStatsRepository();
        userRepo ??= new FakeUserRepository(DefaultUser());
        movieRepo ??= new FakeMovieRepository(DefaultMovie());
        badgeService ??= Mock.Of<IBadgeService>();

        return new PointService(statsRepo, userRepo, movieRepo, badgeService);
    }

    [Fact]
    public async Task GetUserStats_ReturnsExistingStats_WhenStatsAlreadyExist()
    {
        var statsRepo = new FakeUserStatsRepository();
        var user = DefaultUser();
        var existing = new UserStats { User = user, TotalPoints = 42, WeeklyScore = 10 };
        statsRepo.Insert(existing);

        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        var result = await sut.GetUserStats(user.UserId);

        Assert.Equal(42, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_CreatesNewStats_WhenStatsDoNotExist()
    {
        var user = DefaultUser();
        var sut = CreateSut(userRepo: new FakeUserRepository(user));

        var result = await sut.GetUserStats(user.UserId);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        var sut = CreateSut(userRepo: new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetUserStats(999));
    }

    [Fact]
    public async Task AddPoints_AddsTwoPoints_WhenMovieAverageRatingIsAbove3Point5()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 4.0);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(2, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsOnePoint_WhenMovieAverageRatingIsBelow2()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 1.5);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(1, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsZeroPoints_WhenMovieAverageRatingIsBetween2And3Point5()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 3.0);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsZeroPoints_WhenMovieAverageRatingIsExactly2()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 2.0);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsZeroPoints_WhenMovieAverageRatingIsExactly3Point5()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 3.5);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsFiveExtraPoints_WhenIsBattleMovieIsTrue()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 3.0);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: true);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(5, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsCombinedPoints_WhenHighRatingAndIsBattleMovie()
    {
        var user = DefaultUser();
        var movie = DefaultMovie(avgRating: 4.0);
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository(movie));

        await sut.AddPoints(user.UserId, movie.MovieId, isBattleMovie: true);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(7, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_DoesNothing_WhenMovieDoesNotExist()
    {
        var user = DefaultUser();
        var sut = CreateSut(
            userRepo: new FakeUserRepository(user),
            movieRepo: new FakeMovieRepository());

        await sut.AddPoints(user.UserId, 999, isBattleMovie: false);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_ReducesTotalPoints_BySpecifiedAmount()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 20 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await sut.DeductPoints(user.UserId, 8);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(12, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_ClampsToZero_WhenDeductionExceedsTotalPoints()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 5 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await sut.DeductPoints(user.UserId, 100);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_DeductsAmount_WhenUserHasSufficientPoints()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 50 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await sut.FreezePoints(user.UserId, 20);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(30, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_ThrowsInvalidOperationException_WhenPointsAreInsufficient()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 10 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.FreezePoints(user.UserId, 50));
    }

    [Fact]
    public async Task FreezePoints_ThrowsInvalidOperationException_WhenUserHasZeroPoints()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 0 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.FreezePoints(user.UserId, 1));
    }

    [Fact]
    public async Task RefundPoints_IncreasesTotalPoints_BySpecifiedAmount()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 10 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await sut.RefundPoints(user.UserId, 15);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(25, stats.TotalPoints);
    }

    [Fact]
    public async Task UpdateWeeklyScore_SetsWeeklyScore_ToCurrentTotalPoints()
    {
        var user = DefaultUser();
        var statsRepo = new FakeUserStatsRepository();
        statsRepo.Insert(new UserStats { User = user, TotalPoints = 77, WeeklyScore = 0 });
        var sut = CreateSut(statsRepo, new FakeUserRepository(user));

        await sut.UpdateWeeklyScore(user.UserId);

        var stats = await sut.GetUserStats(user.UserId);
        Assert.Equal(77, stats.WeeklyScore);
    }
}