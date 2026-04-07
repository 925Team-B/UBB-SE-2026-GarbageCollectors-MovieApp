using Xunit;
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using MovieApp.Core.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tests.Integration.Services;

public class ReviewServiceIntegrationTests : IntegrationTestBase
{
    private readonly ReviewRepository _reviewRepository;
    private readonly MovieRepository _movieRepository;
    private readonly UserRepository _userRepository;
    private readonly BattleRepository _battleRepository;
    private readonly Mock<IPointService> _pointServiceMock;
    private readonly ReviewService _sut;

    public ReviewServiceIntegrationTests()
    {
        _reviewRepository = new ReviewRepository(ConnectionString);
        _movieRepository = new MovieRepository(ConnectionString);
        _userRepository = new UserRepository(ConnectionString);
        _battleRepository = new BattleRepository(ConnectionString);
        _pointServiceMock = new Mock<IPointService>();
        _sut = new ReviewService(_reviewRepository, _movieRepository, _userRepository, _battleRepository, _pointServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _userRepository.Insert(new User());
        _movieRepository.Insert(new Movie { Title = "Test Movie", Year = 2000, Genre = "Drama", PosterUrl = "", AverageRating = 0 });
    }

    private static string ValidContent(int length = 100) => new('x', length);

    [Fact]
    public async Task AddReview_PersistsReview_WhenInputIsValid()
    {
        var result = await _sut.AddReview(1, 1, 4.0f, ValidContent());

        Assert.NotNull(result);
        Assert.True(result.ReviewId > 0);
    }

    [Fact]
    public async Task AddReview_UpdatesMovieAverageRating_WhenReviewIsAdded()
    {
        await _sut.AddReview(1, 1, 4.0f, ValidContent());

        var movie = _movieRepository.GetById(1);
        Assert.NotEqual(0, movie!.AverageRating);
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddReview(999, 1, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenMovieDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddReview(1, 999, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserAlreadyReviewedMovie()
    {
        await _sut.AddReview(1, 1, 4.0f, ValidContent());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddReview(1, 1, 3.0f, ValidContent()));
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsPersistedReviews()
    {
        await _sut.AddReview(1, 1, 4.0f, ValidContent());

        var result = await _sut.GetReviewsForMovie(1);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsEmpty_WhenNoReviewsExist()
    {
        var result = await _sut.GetReviewsForMovie(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateReview_PersistsChanges_WhenReviewExists()
    {
        var review = await _sut.AddReview(1, 1, 4.0f, ValidContent());

        await _sut.UpdateReview(review.ReviewId, 2.0f, ValidContent(200));

        var updated = _reviewRepository.GetById(review.ReviewId);
        Assert.Equal(2.0f, updated!.StarRating);
    }

    [Fact]
    public async Task UpdateReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateReview(999, 3.0f, ValidContent()));
    }

    [Fact]
    public async Task DeleteReview_RemovesReview_WhenReviewExists()
    {
        var review = await _sut.AddReview(1, 1, 4.0f, ValidContent());

        await _sut.DeleteReview(review.ReviewId);

        Assert.Null(_reviewRepository.GetById(review.ReviewId));
    }

    [Fact]
    public async Task DeleteReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.DeleteReview(999));
    }

    [Fact]
    public async Task GetAverageRating_ReturnsCorrectAverage_ForMultipleReviews()
    {
        _userRepository.Insert(new User());
        await _sut.AddReview(1, 1, 2.0f, ValidContent());
        await _sut.AddReview(2, 1, 4.0f, ValidContent());

        var result = await _sut.GetAverageRating(1);

        Assert.Equal(3.0, result);
    }

    [Fact]
    public async Task GetAverageRating_ReturnsZero_WhenNoReviewsExist()
    {
        var result = await _sut.GetAverageRating(1);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SubmitExtraReview_SetsIsExtraReviewToTrue_WhenInputIsValid()
    {
        var review = await _sut.AddReview(1, 1, 4.0f, ValidContent());

        await _sut.SubmitExtraReview(
            review.ReviewId,
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            ValidContent(600));

        var updated = _reviewRepository.GetById(review.ReviewId);
        Assert.True(updated!.IsExtraReview);
    }
}

public class PointServiceIntegrationTests : IntegrationTestBase
{
    private readonly UserStatsRepository _userStatsRepository;
    private readonly UserRepository _userRepository;
    private readonly MovieRepository _movieRepository;
    private readonly Mock<IBadgeService> _badgeServiceMock;
    private readonly PointService _sut;

    public PointServiceIntegrationTests()
    {
        _userStatsRepository = new UserStatsRepository(ConnectionString);
        _userRepository = new UserRepository(ConnectionString);
        _movieRepository = new MovieRepository(ConnectionString);
        _badgeServiceMock = new Mock<IBadgeService>();
        _sut = new PointService(_userStatsRepository, _userRepository, _movieRepository, _badgeServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        _userRepository.Insert(new User());
        _movieRepository.Insert(new Movie { Title = "Test Movie", Year = 2000, Genre = "Drama", PosterUrl = "", AverageRating = 4.0 });
    }

    [Fact]
    public async Task GetUserStats_CreatesStats_WhenUserExistsButStatsDoNot()
    {
        var result = await _sut.GetUserStats(1);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_ReturnsExistingStats_WhenStatsAlreadyExist()
    {
        await _sut.GetUserStats(1);
        await _sut.AddPoints(1, 1, isBattleMovie: false);

        var result = await _sut.GetUserStats(1);

        Assert.Equal(2, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetUserStats(999));
    }

    [Fact]
    public async Task AddPoints_PersistsPoints_WhenMovieRatingIsAbove3Point5()
    {
        await _sut.AddPoints(1, 1, isBattleMovie: false);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(2, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsBattleBonus_WhenIsBattleMovieIsTrue()
    {
        await _sut.AddPoints(1, 1, isBattleMovie: true);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(7, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_PersistsDeduction_WhenUserHasPoints()
    {
        await _sut.AddPoints(1, 1, isBattleMovie: false);
        await _sut.DeductPoints(1, 1);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(1, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_ClampsToZero_WhenDeductionExceedsTotal()
    {
        await _sut.DeductPoints(1, 100);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_DeductsPoints_WhenUserHasSufficientPoints()
    {
        await _sut.AddPoints(1, 1, isBattleMovie: true);
        await _sut.FreezePoints(1, 5);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(2, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_ThrowsInvalidOperationException_WhenPointsAreInsufficient()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.FreezePoints(1, 100));
    }

    [Fact]
    public async Task RefundPoints_IncreasesPoints_BySpecifiedAmount()
    {
        await _sut.RefundPoints(1, 10);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(10, stats.TotalPoints);
    }

    [Fact]
    public async Task UpdateWeeklyScore_SetsWeeklyScore_ToCurrentTotalPoints()
    {
        await _sut.AddPoints(1, 1, isBattleMovie: false);
        await _sut.UpdateWeeklyScore(1);

        var stats = await _sut.GetUserStats(1);
        Assert.Equal(stats.TotalPoints, stats.WeeklyScore);
    }
}