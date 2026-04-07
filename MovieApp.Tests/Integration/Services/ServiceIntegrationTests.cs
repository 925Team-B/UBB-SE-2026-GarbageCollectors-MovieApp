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
    private readonly ReviewRepository reviewRepository;
    private readonly MovieRepository movieRepository;
    private readonly UserRepository userRepository;
    private readonly BattleRepository battleRepository;
    private readonly Mock<IPointService> pointServiceMock;
    private readonly ReviewService sut;

    public ReviewServiceIntegrationTests()
    {
        reviewRepository = new ReviewRepository(ConnectionString);
        movieRepository = new MovieRepository(ConnectionString);
        userRepository = new UserRepository(ConnectionString);
        battleRepository = new BattleRepository(ConnectionString);
        pointServiceMock = new Mock<IPointService>();
        sut = new ReviewService(reviewRepository, movieRepository, userRepository, battleRepository, pointServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        userRepository.Insert(new User());
        movieRepository.Insert(new Movie { Title = "Test Movie", Year = 2000, Genre = "Drama", PosterUrl = string.Empty, AverageRating = 0 });
    }

    private static string ValidContent(int length = 100) => new ('x', length);

    [Fact]
    public async Task AddReview_PersistsReview_WhenInputIsValid()
    {
        var result = await sut.AddReview(1, 1, 4.0f, ValidContent());

        Assert.NotNull(result);
        Assert.True(result.ReviewId > 0);
    }

    [Fact]
    public async Task AddReview_UpdatesMovieAverageRating_WhenReviewIsAdded()
    {
        await sut.AddReview(1, 1, 4.0f, ValidContent());

        var movie = movieRepository.GetById(1);
        Assert.NotEqual(0, movie!.AverageRating);
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(999, 1, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenMovieDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 999, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserAlreadyReviewedMovie()
    {
        await sut.AddReview(1, 1, 4.0f, ValidContent());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 1, 3.0f, ValidContent()));
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsPersistedReviews()
    {
        await sut.AddReview(1, 1, 4.0f, ValidContent());

        var result = await sut.GetReviewsForMovie(1);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsEmpty_WhenNoReviewsExist()
    {
        var result = await sut.GetReviewsForMovie(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateReview_PersistsChanges_WhenReviewExists()
    {
        var review = await sut.AddReview(1, 1, 4.0f, ValidContent());

        await sut.UpdateReview(review.ReviewId, 2.0f, ValidContent(200));

        var updated = reviewRepository.GetById(review.ReviewId);
        Assert.Equal(2.0f, updated!.StarRating);
    }

    [Fact]
    public async Task UpdateReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateReview(999, 3.0f, ValidContent()));
    }

    [Fact]
    public async Task DeleteReview_RemovesReview_WhenReviewExists()
    {
        var review = await sut.AddReview(1, 1, 4.0f, ValidContent());

        await sut.DeleteReview(review.ReviewId);

        Assert.Null(reviewRepository.GetById(review.ReviewId));
    }

    [Fact]
    public async Task DeleteReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.DeleteReview(999));
    }

    [Fact]
    public async Task GetAverageRating_ReturnsCorrectAverage_ForMultipleReviews()
    {
        userRepository.Insert(new User());
        await sut.AddReview(1, 1, 2.0f, ValidContent());
        await sut.AddReview(2, 1, 4.0f, ValidContent());

        var result = await sut.GetAverageRating(1);

        Assert.Equal(3.0, result);
    }

    [Fact]
    public async Task GetAverageRating_ReturnsZero_WhenNoReviewsExist()
    {
        var result = await sut.GetAverageRating(1);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SubmitExtraReview_SetsIsExtraReviewToTrue_WhenInputIsValid()
    {
        var review = await sut.AddReview(1, 1, 4.0f, ValidContent());

        await sut.SubmitExtraReview(
            review.ReviewId,
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            3, ValidContent(60),
            ValidContent(600));

        var updated = reviewRepository.GetById(review.ReviewId);
        Assert.True(updated!.IsExtraReview);
    }
}

public class PointServiceIntegrationTests : IntegrationTestBase
{
    private readonly UserStatsRepository userStatsRepository;
    private readonly UserRepository userRepository;
    private readonly MovieRepository movieRepository;
    private readonly Mock<IBadgeService> badgeServiceMock;
    private readonly PointService sut;

    public PointServiceIntegrationTests()
    {
        userStatsRepository = new UserStatsRepository(ConnectionString);
        userRepository = new UserRepository(ConnectionString);
        movieRepository = new MovieRepository(ConnectionString);
        badgeServiceMock = new Mock<IBadgeService>();
        sut = new PointService(userStatsRepository, userRepository, movieRepository, badgeServiceMock.Object);

        SeedData();
    }

    private void SeedData()
    {
        userRepository.Insert(new User());
        movieRepository.Insert(new Movie { Title = "Test Movie", Year = 2000, Genre = "Drama", PosterUrl = string.Empty, AverageRating = 4.0 });
    }

    [Fact]
    public async Task GetUserStats_CreatesStats_WhenUserExistsButStatsDoNot()
    {
        var result = await sut.GetUserStats(1);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_ReturnsExistingStats_WhenStatsAlreadyExist()
    {
        await sut.GetUserStats(1);
        await sut.AddPoints(1, 1, isBattleMovie: false);

        var result = await sut.GetUserStats(1);

        Assert.Equal(2, result.TotalPoints);
    }

    [Fact]
    public async Task GetUserStats_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetUserStats(999));
    }

    [Fact]
    public async Task AddPoints_PersistsPoints_WhenMovieRatingIsAbove3Point5()
    {
        await sut.AddPoints(1, 1, isBattleMovie: false);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(2, stats.TotalPoints);
    }

    [Fact]
    public async Task AddPoints_AddsBattleBonus_WhenIsBattleMovieIsTrue()
    {
        await sut.AddPoints(1, 1, isBattleMovie: true);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(7, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_PersistsDeduction_WhenUserHasPoints()
    {
        await sut.AddPoints(1, 1, isBattleMovie: false);
        await sut.DeductPoints(1, 1);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(1, stats.TotalPoints);
    }

    [Fact]
    public async Task DeductPoints_ClampsToZero_WhenDeductionExceedsTotal()
    {
        await sut.DeductPoints(1, 100);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(0, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_DeductsPoints_WhenUserHasSufficientPoints()
    {
        await sut.AddPoints(1, 1, isBattleMovie: true);
        await sut.FreezePoints(1, 5);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(2, stats.TotalPoints);
    }

    [Fact]
    public async Task FreezePoints_ThrowsInvalidOperationException_WhenPointsAreInsufficient()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.FreezePoints(1, 100));
    }

    [Fact]
    public async Task RefundPoints_IncreasesPoints_BySpecifiedAmount()
    {
        await sut.RefundPoints(1, 10);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(10, stats.TotalPoints);
    }

    [Fact]
    public async Task UpdateWeeklyScore_SetsWeeklyScore_ToCurrentTotalPoints()
    {
        await sut.AddPoints(1, 1, isBattleMovie: false);
        await sut.UpdateWeeklyScore(1);

        var stats = await sut.GetUserStats(1);
        Assert.Equal(stats.TotalPoints, stats.WeeklyScore);
    }
}