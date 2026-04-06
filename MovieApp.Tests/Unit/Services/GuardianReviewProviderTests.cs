#nullable enable
using System.Net;
using System.Text.Json;
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models.DTOs;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class GuardianReviewProviderTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly HttpClient _httpClient;
    private readonly GuardianReviewProvider _sut;

    public GuardianReviewProviderTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        // HttpClient won't be called in these tests because ICacheService is fully mocked
        _httpClient = new HttpClient();
        _sut = new GuardianReviewProvider(_httpClient, _cacheServiceMock.Object);
    }

    private static string BuildGuardianJson(string webTitle, string webUrl, string trailText)
    {
        var dto = new GuardianApiResponseDto
        {
            Response = new GuardianResponseDto
            {
                Results = new List<GuardianResultDto>
                {
                    new()
                    {
                        WebTitle = webTitle,
                        WebUrl = webUrl,
                        Fields = new GuardianFieldsDto { TrailText = trailText }
                    }
                }
            }
        };
        return JsonSerializer.Serialize(dto);
    }

    // --- GetReviewAsync ---

    [Fact]
    public async Task GetReviewAsync_WhenCacheReturnsMatchingResult_ReturnsPopulatedCriticReview()
    {
        var json = BuildGuardianJson("Inception review – Christopher Nolan", "https://guardian.com/inception", "A mind-bending film.");
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(json);

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
        Assert.Equal("The Guardian", result.Source);
        Assert.Contains("Inception", result.Headline);
        Assert.Contains("https://guardian.com/inception", result.Url);
    }

    [Fact]
    public async Task GetReviewAsync_WhenMovieTitleIsWhitespace_ReturnsNull()
    {
        var result = await _sut.GetReviewAsync("   ", 2010);

        Assert.Null(result);
        _cacheServiceMock.Verify(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()), Times.Never);
    }

    [Fact]
    public async Task GetReviewAsync_WhenMovieTitleIsEmpty_ReturnsNull()
    {
        var result = await _sut.GetReviewAsync(string.Empty, 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_WhenCacheReturnsEmptyJson_ReturnsNull()
    {
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(string.Empty);

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_WhenCacheReturnsWhitespace_ReturnsNull()
    {
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync("   ");

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_WhenCacheReturnsNoResults_ReturnsNull()
    {
        var dto = new GuardianApiResponseDto
        {
            Response = new GuardianResponseDto { Results = new List<GuardianResultDto>() }
        };
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(JsonSerializer.Serialize(dto));

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_WhenResultDoesNotMatchMovieTitle_ReturnsNull()
    {
        // A result whose headline and trail text have zero match with the requested movie
        var json = BuildGuardianJson("Some completely unrelated article", "https://guardian.com/unrelated", "Nothing to do with the requested film.");
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(json);

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReviewAsync_WhenTrailTextIsEmpty_SnippetContainsFallbackText()
    {
        var json = BuildGuardianJson("Inception review", "https://guardian.com/inception", string.Empty);
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpClient>()))
            .ReturnsAsync(json);

        var result = await _sut.GetReviewAsync("Inception", 2010);

        Assert.NotNull(result);
        Assert.Contains("The Guardian returned a matching film review article.", result.Snippet);
    }

    [Fact]
    public async Task GetReviewAsync_WhenCalled_PassesHttpClientToCacheService()
    {
        var json = BuildGuardianJson("Inception review", "https://guardian.com/inception", "Great movie.");
        _cacheServiceMock.Setup(c => c.FetchOrCacheAsync(It.IsAny<string>(), It.IsAny<string>(), _httpClient))
            .ReturnsAsync(json);

        await _sut.GetReviewAsync("Inception", 2010);

        _cacheServiceMock.Verify(c => c.FetchOrCacheAsync(
            It.Is<string>(k => k.Contains("guardian") && k.Contains("inception")),
            It.Is<string>(u => u.Contains("guardianapis.com")),
            _httpClient), Times.Once);
    }
}
