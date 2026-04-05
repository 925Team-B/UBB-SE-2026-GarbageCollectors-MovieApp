#nullable enable
using Moq;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

public class ExternalReviewServiceTests
{
    // --- GetExternalReviews ---

    [Fact]
    public async Task GetExternalReviews_WhenProviderReturnsReview_ReturnsListContainingReview()
    {
        var review = new CriticReview { Source = "The Guardian", Headline = "Great film", Snippet = "Amazing.", Score = 0 };
        var providerMock = new Mock<IExternalReviewProvider>();
        providerMock.Setup(p => p.GetReviewAsync("Inception", 2010)).ReturnsAsync(review);

        var sut = new ExternalReviewService(new[] { providerMock.Object });

        var result = await sut.GetExternalReviews("Inception", 2010);

        Assert.Single(result);
        Assert.Equal("The Guardian", result[0].Source);
    }

    [Fact]
    public async Task GetExternalReviews_WhenProviderThrowsException_SkipsFailedProviderAndReturnsOthers()
    {
        var failingProvider = new Mock<IExternalReviewProvider>();
        failingProvider.Setup(p => p.GetReviewAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var successfulReview = new CriticReview { Source = "NYT", Headline = "Brilliant", Snippet = "A masterpiece." };
        var successfulProvider = new Mock<IExternalReviewProvider>();
        successfulProvider.Setup(p => p.GetReviewAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(successfulReview);

        var sut = new ExternalReviewService(new[] { failingProvider.Object, successfulProvider.Object });

        var result = await sut.GetExternalReviews("Inception", 2010);

        Assert.Single(result);
        Assert.Equal("NYT", result[0].Source);
    }

    [Fact]
    public async Task GetExternalReviews_WhenNoProviders_ReturnsEmptyList()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = await sut.GetExternalReviews("Inception", 2010);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExternalReviews_WhenProviderReturnsNull_ExcludesNullFromResult()
    {
        var providerMock = new Mock<IExternalReviewProvider>();
        providerMock.Setup(p => p.GetReviewAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((CriticReview?)null);

        var sut = new ExternalReviewService(new[] { providerMock.Object });

        var result = await sut.GetExternalReviews("Unknown", 2000);

        Assert.Empty(result);
    }

    // --- GetAggregateScores ---

    [Fact]
    public async Task GetAggregateScores_WhenCalledWithTitle_ReturnsBothScoresInValidRange()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var (criticScore, audienceScore) = await sut.GetAggregateScores("Inception");

        Assert.InRange(criticScore, 1.0, 5.0);
        Assert.InRange(audienceScore, 1.0, 5.0);
    }

    [Fact]
    public async Task GetAggregateScores_WhenCalledWithSameTitle_ReturnsDeterministicScores()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var (criticScore1, audienceScore1) = await sut.GetAggregateScores("Inception");
        var (criticScore2, audienceScore2) = await sut.GetAggregateScores("Inception");

        Assert.Equal(criticScore1, criticScore2);
        Assert.Equal(audienceScore1, audienceScore2);
    }

    // --- AnalyseLexicon ---

    [Fact]
    public void AnalyseLexicon_WhenReviewsContainWords_ReturnsTopWordsByFrequency()
    {
        var reviews = new List<CriticReview>
        {
            new() { Headline = "amazing film masterpiece", Snippet = "film amazing amazing" },
            new() { Headline = "brilliant film", Snippet = "stunning visuals" }
        };
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.AnalyseLexicon(reviews);

        Assert.NotEmpty(result);
        Assert.True(result.Count <= 10);
        // "amazing" appears 3 times, "film" appears 3 times — both should be top
        Assert.Contains(result, w => w.Word == "amazing" && w.Count == 3);
        Assert.Contains(result, w => w.Word == "film" && w.Count == 3);
    }

    [Fact]
    public void AnalyseLexicon_WhenReviewsContainOnlyStopWords_ReturnsEmptyList()
    {
        var reviews = new List<CriticReview>
        {
            new() { Headline = "the a an and", Snippet = "it is this that" }
        };
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.AnalyseLexicon(reviews);

        Assert.Empty(result);
    }

    [Fact]
    public void AnalyseLexicon_WhenNoReviews_ReturnsEmptyList()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.AnalyseLexicon(new List<CriticReview>());

        Assert.Empty(result);
    }

    [Fact]
    public void AnalyseLexicon_WithMoreThan10UniqueWords_ReturnsAtMost10Words()
    {
        var words = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"word{i}"));
        var reviews = new List<CriticReview>
        {
            new() { Headline = words, Snippet = string.Empty }
        };
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.AnalyseLexicon(reviews);

        Assert.True(result.Count <= 10);
    }

    // --- IsPolarized ---

    [Fact]
    public void IsPolarized_WhenScoresDifferByMoreThanThreshold_ReturnsTrue()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.IsPolarized(5.0, 2.5);

        Assert.True(result);
    }

    [Fact]
    public void IsPolarized_WhenScoresDifferByLessThanThreshold_ReturnsFalse()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.IsPolarized(4.0, 3.5);

        Assert.False(result);
    }

    [Fact]
    public void IsPolarized_WhenScoresDifferByExactlyThreshold_ReturnsFalse()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        var result = sut.IsPolarized(4.0, 2.0); // diff == 2.0, threshold == 2.0

        Assert.False(result); // not strictly greater
    }

    [Fact]
    public void IsPolarized_WithCustomThreshold_UsesProvidedThreshold()
    {
        var sut = new ExternalReviewService(Enumerable.Empty<IExternalReviewProvider>());

        Assert.True(sut.IsPolarized(5.0, 4.0, threshold: 0.5));
        Assert.False(sut.IsPolarized(5.0, 4.8, threshold: 0.5));
    }
}
