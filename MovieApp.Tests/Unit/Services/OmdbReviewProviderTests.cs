using System.Text.Json;
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Services;

namespace Tests.Unit.Services;

public class OmdbReviewProviderTests
{
    private readonly Mock<ICacheService> cacheServiceMock = new ();
    private readonly HttpClient httpClient = new ();

    private OmdbReviewProvider CreateSut() =>
        new (httpClient, cacheServiceMock.Object);

    private void SetupCache(string json) =>
        cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(json);

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenMovieTitleIsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetReviewAsync(null!, 2020);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenMovieTitleIsWhitespace()
    {
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("   ", 2020);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenCacheReturnsEmptyString()
    {
        SetupCache(string.Empty);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenRatingsArrayIsEmpty()
    {
        SetupCache(JsonSerializer.Serialize(new { Ratings = Array.Empty<object>() }));
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenResponseJsonIsMalformed()
    {
        SetupCache("{ not valid json }}}");
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenResponseIsEmptyJson()
    {
        SetupCache("{}");
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsCriticReview_WhenRatingsArePresent()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Internet Movie Database", Value = "8.8/10" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetReviewAsync_SetsSource_FromFirstRatingEntry()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "87%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal("Rotten Tomatoes", result!.Source);
    }

    [Fact]
    public async Task GetReviewAsync_ParsesPercentageScore_CorrectlyToFivePointScale()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "100%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(5.0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ParsesPercentageScore_AtMidpoint()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "60%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(3.0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ParsesFractionScore_OutOfTen()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Internet Movie Database", Value = "8.8/10" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(4.4, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ParsesFractionScore_OutOfHundred()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Metacritic", Value = "74/100" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(3.7, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsScoreZero_WhenValueFormatIsUnrecognised()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Some Source", Value = "excellent" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsScoreZero_WhenValueIsEmpty()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Some Source", Value = string.Empty } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsScoreZero_WhenFractionHasNonNumericParts()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Some Source", Value = "abc/10" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_ClampsScore_ToMaximumOfFive()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "200%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(5.0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_SnippetContainsMovieTitle()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "87%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Contains("Inception", result!.Snippet);
    }

    [Fact]
    public async Task GetReviewAsync_UrlContainsEncodedMovieTitle()
    {
        var json = JsonSerializer.Serialize(new
        {
            Ratings = new[] { new { Source = "Rotten Tomatoes", Value = "87%" } }
        });
        SetupCache(json);
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Contains("Inception", result!.Url);
    }
}