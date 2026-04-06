using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using MovieApp.Core.Services;

namespace Tests.Unit.Services;

internal class FakeReviewRepository : ReviewRepository
{
    private readonly List<Review> _store = new();
    private int _nextId = 1;

    public FakeReviewRepository(params Review[] seed) : base(string.Empty)
    {
        foreach (var r in seed)
        {
            r.ReviewId = _nextId++;
            _store.Add(r);
        }
    }

    public override List<Review> GetAll() => _store.ToList();

    public override Review? GetById(int id) => _store.FirstOrDefault(r => r.ReviewId == id);

    public override int Insert(Review review)
    {
        review.ReviewId = _nextId++;
        _store.Add(review);
        return review.ReviewId;
    }

    public override bool Update(Review review)
    {
        var index = _store.FindIndex(r => r.ReviewId == review.ReviewId);
        if (index < 0) return false;
        _store[index] = review;
        return true;
    }

    public override bool Delete(int id)
    {
        var review = _store.FirstOrDefault(r => r.ReviewId == id);
        if (review is null) return false;
        _store.Remove(review);
        return true;
    }
}

internal class FakeMovieRepositoryForReview : MovieRepository
{
    private readonly Dictionary<int, Movie> _store;

    public FakeMovieRepositoryForReview(params Movie[] movies) : base(string.Empty)
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

internal class FakeUserRepositoryForReview : UserRepository
{
    private readonly Dictionary<int, User> _store;

    public FakeUserRepositoryForReview(params User[] users) : base(string.Empty)
    {
        _store = users.ToDictionary(u => u.UserId);
    }

    public override User? GetById(int id) =>
        _store.TryGetValue(id, out var u) ? u : null;
}

internal class FakeBattleRepository : BattleRepository
{
    private readonly List<Battle> _store;

    public FakeBattleRepository(params Battle[] battles) : base(string.Empty)
    {
        _store = battles.ToList();
    }

    public override List<Battle> GetAll() => _store.ToList();
}

public class ReviewServiceTests
{
    private static User DefaultUser(int id = 1) => new() { UserId = id };
    private static Movie DefaultMovie(int id = 10) => new() { MovieId = id, Title = "Test Movie", AverageRating = 3.0 };

    private static string ValidContent(int length = 100) => new('x', length);

    private ReviewService CreateSut(
        FakeReviewRepository? reviewRepo = null,
        FakeMovieRepositoryForReview? movieRepo = null,
        FakeUserRepositoryForReview? userRepo = null,
        FakeBattleRepository? battleRepo = null,
        IPointService? pointService = null)
    {
        reviewRepo ??= new FakeReviewRepository();
        movieRepo ??= new FakeMovieRepositoryForReview(DefaultMovie());
        userRepo ??= new FakeUserRepositoryForReview(DefaultUser());
        battleRepo ??= new FakeBattleRepository();
        pointService ??= Mock.Of<IPointService>();

        return new ReviewService(reviewRepo, movieRepo, userRepo, battleRepo, pointService);
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsOnlyReviewsForSpecifiedMovie()
    {
        var movie1 = DefaultMovie(10);
        var movie2 = DefaultMovie(20);
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie1, StarRating = 4f, Content = ValidContent() },
            new Review { User = user, Movie = movie2, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo, new FakeMovieRepositoryForReview(movie1, movie2));

        var result = await sut.GetReviewsForMovie(movie1.MovieId);

        Assert.Single(result);
        Assert.Equal(movie1.MovieId, result[0].Movie!.MovieId);
    }

    [Fact]
    public async Task GetReviewsForMovie_ReturnsEmptyList_WhenNoReviewsExist()
    {
        var sut = CreateSut();

        var result = await sut.GetReviewsForMovie(10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReviewsForMovie_ExcludesReviews_WithStarRatingAboveFive()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 6f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        var result = await sut.GetReviewsForMovie(movie.MovieId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddReview_ReturnsReview_WhenInputIsValid()
    {
        var sut = CreateSut();

        var result = await sut.AddReview(1, 10, 4.0f, ValidContent());

        Assert.NotNull(result);
        Assert.Equal(4.0f, result.StarRating);
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserDoesNotExist()
    {
        var sut = CreateSut(userRepo: new FakeUserRepositoryForReview());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(999, 10, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenMovieDoesNotExist()
    {
        var sut = CreateSut(movieRepo: new FakeMovieRepositoryForReview());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 999, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenUserAlreadyReviewedMovie()
    {
        var user = DefaultUser();
        var movie = DefaultMovie();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(user.UserId, movie.MovieId, 4.0f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenRatingIsNegative()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 10, -0.5f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenRatingExceedsFive()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 10, 5.5f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenRatingIsNotHalfIncrement()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 10, 3.3f, ValidContent()));
    }

    [Fact]
    public async Task AddReview_ThrowsInvalidOperationException_WhenContentExceeds2000Characters()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddReview(1, 10, 4.0f, ValidContent(2001)));
    }

    [Fact]
    public async Task AddReview_Succeeds_WhenRatingIsZero()
    {
        var sut = CreateSut();

        var result = await sut.AddReview(1, 10, 0f, ValidContent());

        Assert.Equal(0f, result.StarRating);
    }

    [Fact]
    public async Task AddReview_Succeeds_WhenRatingIsExactlyFive()
    {
        var sut = CreateSut();

        var result = await sut.AddReview(1, 10, 5.0f, ValidContent());

        Assert.Equal(5.0f, result.StarRating);
    }

    [Fact]
    public async Task GetAverageRating_ReturnsZero_WhenNoReviewsExist()
    {
        var sut = CreateSut();

        var result = await sut.GetAverageRating(10);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetAverageRating_ReturnsCorrectAverage_ForMultipleReviews()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 2f, Content = ValidContent() },
            new Review { User = DefaultUser(2), Movie = movie, StarRating = 4f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        var result = await sut.GetAverageRating(movie.MovieId);

        Assert.Equal(3.0, result);
    }

    [Fact]
    public async Task UpdateReview_UpdatesRatingAndContent_WhenReviewExists()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        await sut.UpdateReview(1, 4.5f, ValidContent(200));

        var updated = repo.GetById(1)!;
        Assert.Equal(4.5f, updated.StarRating);
    }

    [Fact]
    public async Task UpdateReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateReview(999, 3.0f, ValidContent()));
    }

    [Fact]
    public async Task UpdateReview_ThrowsInvalidOperationException_WhenNewRatingIsInvalid()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateReview(1, 6.0f, ValidContent()));
    }

    [Fact]
    public async Task UpdateReview_ThrowsInvalidOperationException_WhenNewContentExceeds2000Characters()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateReview(1, 3.0f, ValidContent(2001)));
    }

    [Fact]
    public async Task DeleteReview_RemovesReview_WhenReviewExists()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);

        await sut.DeleteReview(1);

        Assert.Null(repo.GetById(1));
    }

    [Fact]
    public async Task DeleteReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteReview(999));
    }

    [Fact]
    public async Task SubmitExtraReview_SetsIsExtraReviewToTrue_WhenInputIsValid()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);
        var longText = ValidContent(600);
        var catText = ValidContent(60);

        await sut.SubmitExtraReview(1, 3, catText, 3, catText, 3, catText, 3, catText, 3, catText, longText);

        Assert.True(repo.GetById(1)!.IsExtraReview);
    }

    [Fact]
    public async Task SubmitExtraReview_ThrowsInvalidOperationException_WhenMainTextIsTooShort()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);
        var catText = ValidContent(60);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitExtraReview(1, 3, catText, 3, catText, 3, catText, 3, catText, 3, catText, ValidContent(100)));
    }

    [Fact]
    public async Task SubmitExtraReview_ThrowsInvalidOperationException_WhenMainTextIsTooLong()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);
        var catText = ValidContent(60);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitExtraReview(1, 3, catText, 3, catText, 3, catText, 3, catText, 3, catText, ValidContent(12001)));
    }

    [Fact]
    public async Task SubmitExtraReview_ThrowsInvalidOperationException_WhenCategoryTextIsTooShort()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);
        var longText = ValidContent(600);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitExtraReview(1, 3, ValidContent(10), 3, ValidContent(60), 3, ValidContent(60), 3, ValidContent(60), 3, ValidContent(60), longText));
    }

    [Fact]
    public async Task SubmitExtraReview_ThrowsInvalidOperationException_WhenCategoryRatingExceedsFive()
    {
        var movie = DefaultMovie();
        var user = DefaultUser();
        var repo = new FakeReviewRepository(
            new Review { User = user, Movie = movie, StarRating = 3f, Content = ValidContent() }
        );
        var sut = CreateSut(repo);
        var longText = ValidContent(600);
        var catText = ValidContent(60);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitExtraReview(1, 6, catText, 3, catText, 3, catText, 3, catText, 3, catText, longText));
    }

    [Fact]
    public async Task SubmitExtraReview_ThrowsInvalidOperationException_WhenReviewDoesNotExist()
    {
        var sut = CreateSut();
        var catText = ValidContent(60);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SubmitExtraReview(999, 3, catText, 3, catText, 3, catText, 3, catText, 3, catText, ValidContent(600)));
    }
}