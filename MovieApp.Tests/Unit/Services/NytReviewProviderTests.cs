using System.Text.Json;
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Services;

namespace Tests.Unit.Services;

public class NytReviewProviderTests
{
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly HttpClient _httpClient = new();

    private NytReviewProvider CreateSut() =>
        new(_httpClient, _cacheServiceMock.Object);

    private static string BuildNytJson(string headline, string snippet, string webUrl = "https://nytimes.com/review")
    {
        return JsonSerializer.Serialize(new
        {
            response = new
            {
                docs = new[]
                {
                    new
                    {
                        headline = new { main = headline },
                        snippet,
                        web_url = webUrl
                    }
                }
            }
        });
    }

    private static string EmptyNytJson() =>
        JsonSerializer.Serialize(new { response = new { docs = Array.Empty<object>() } });

    private static string OmdbContextJson(string title, string year, string director) =>
        JsonSerializer.Serialize(new { Title = title, Year = year, Director = director });

    private void SetupCacheSequence(string omdbJson, string nytJson)
    {
        var callCount = 0;
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(() => callCount++ == 0 ? omdbJson : nytJson);
    }

    private void SetupCache(string json)
    {
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(json);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenMovieTitleIsNull()
    {
        var sut = CreateSut();

        var result = await sut.GetReviewAsync(null!, 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenMovieTitleIsWhitespace()
    {
        var sut = CreateSut();

        var result = await sut.GetReviewAsync("   ", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenAllSearchVariantsReturnEmptyDocs()
    {
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(EmptyNytJson());

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenCacheReturnsEmptyString()
    {
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(string.Empty);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenNytJsonIsMalformed()
    {
        SetupCacheSequence(
            OmdbContextJson("Inception", "2010", "Christopher Nolan"),
            "{ bad json }}}"
        );

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
    public async Task GetReviewAsync_ReturnsNull_WhenNoDocMatchesMovieTitle()
    {
        var nytJson = BuildNytJson(
            headline: "A Review of Interstellar",
            snippet: "Interstellar 2014 film review"
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsCriticReview_WhenDocMatchesMovieTitleAndYear()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review."
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetReviewAsync_SetsSourceToNewYorkTimes_WhenMatchFound()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review."
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal("New York Times", result!.Source);
    }

    [Fact]
    public async Task GetReviewAsync_SetsHeadline_FromMatchedDoc()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review."
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal("Inception review: Christopher Nolan film", result!.Headline);
    }

    [Fact]
    public async Task GetReviewAsync_SetsUrl_FromMatchedDoc()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review.",
            webUrl: "https://nytimes.com/inception-review"
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal("https://nytimes.com/inception-review", result!.Url);
    }

    [Fact]
    public async Task GetReviewAsync_SnippetContainsMovieTitle_WhenMatchFound()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review."
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Contains("Inception", result!.Snippet);
    }

    [Fact]
    public async Task GetReviewAsync_ScoreIsZero_BecauseNytDoesNotProvideNumericScore()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review: Christopher Nolan film",
            snippet: "Inception (2010) is a mind-bending movie review."
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Equal(0, result!.Score);
    }

    [Fact]
    public async Task GetReviewAsync_FallsBackToOriginalTitle_WhenOmdbContextReturnsEmpty()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review film 2010",
            snippet: "Inception 2010 movie review film"
        );

        var callCount = 0;
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(() => callCount++ == 0 ? string.Empty : nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenOmdbJsonIsMalformed()
    {
        SetupCacheSequence("{ bad omdb json }}}", EmptyNytJson());

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenMatchedDocHasZeroMatchScore()
    {
        var nytJson = BuildNytJson(
            headline: "",
            snippet: ""
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenDocIsGenericRoundupWithoutMovieTitle()
    {
        var nytJson = BuildNytJson(
            headline: "Best movies of 2010 ranked streaming",
            snippet: "Best movies of 2010 ranked streaming list"
        );
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_ReturnsNull_WhenDocPassesFilterButMatchScoreIsZero()
    {
        var nytJson = JsonSerializer.Serialize(new
        {
            response = new
            {
                docs = new[]
                {
                    new
                    {
                        headline = new { main = (string?)null },
                        snippet = (string?)null,
                        web_url = "https://nytimes.com/review"
                    }
                }
            }
        });
        SetupCacheSequence(OmdbContextJson("Inception", "2010", "Christopher Nolan"), nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_FallsBackToOriginalTitle_WhenOmdbReturnsNullDto()
    {
        var nytJson = BuildNytJson(
            headline: "Inception review film 2010",
            snippet: "Inception 2010 movie review film"
        );

        var callCount = 0;
        _cacheServiceMock
            .Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(() => callCount++ == 0 ? "null" : nytJson);

        var sut = CreateSut();

        var result = await sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
    }
}