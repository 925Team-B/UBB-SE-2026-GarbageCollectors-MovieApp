#nullable enable
using System.Text.Json;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Models.DTOs;

namespace MovieApp.Core.Services;

public sealed class GuardianReviewProvider : IExternalReviewProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;

    public GuardianReviewProvider(HttpClient httpClient, ICacheService cacheService)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
    }

    public async Task<CriticReview?> GetReviewAsync(string movieTitle, int releaseYear)
    {
        if (string.IsNullOrWhiteSpace(movieTitle))
            return null;

        var query = Uri.EscapeDataString(movieTitle);
        var url = $"https://content.guardianapis.com/search?q={query}&section=film&tag=tone/reviews&show-fields=trailText&page-size=10&from-date={releaseYear}-01-01&to-date={releaseYear + 1}-12-31&api-key=df72ccc6-affe-4757-a0a8-fca2eecd0cc5";
        var cacheKey = BuildCacheKey("guardian", movieTitle, releaseYear);

        var json = await _cacheService.FetchOrCacheAsync(cacheKey, url, _httpClient);
        if (string.IsNullOrWhiteSpace(json))
            return null;
        var dto = JsonSerializer.Deserialize<GuardianApiResponseDto>(json);

        var result = dto?.Response?.Results?
            .OrderByDescending(r => MatchScore(movieTitle, releaseYear, r.WebTitle, r.Fields?.TrailText))
            .FirstOrDefault();
        if (result is null)
            return null;

        if (MatchScore(movieTitle, releaseYear, result.WebTitle, result.Fields?.TrailText) <= 0)
            return null;

        return new CriticReview
        {
            Source = "The Guardian",
            Score = 0,
            Headline = result.WebTitle,
            Snippet = BuildLongerSnippet(result.Fields?.TrailText, movieTitle, releaseYear),
            Url = result.WebUrl
        };
    }

    private static string BuildCacheKey(string provider, string movieTitle, int releaseYear)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(movieTitle.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return $"{provider}_{sanitized}_{releaseYear}_v2".Replace(' ', '_').ToLowerInvariant();
    }

    private static int MatchScore(string movieTitle, int releaseYear, string? headline, string? snippet)
    {
        var text = $"{headline} {snippet}";
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var score = 0;
        if (text.Contains(movieTitle, StringComparison.OrdinalIgnoreCase))
            score += 10;

        var tokens = movieTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToList();

        score += tokens.Count(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

        if (text.Contains(releaseYear.ToString(), StringComparison.OrdinalIgnoreCase))
            score += 2;

        return score;
    }

    private static string BuildLongerSnippet(string? trailText, string movieTitle, int releaseYear)
    {
        var baseSnippet = string.IsNullOrWhiteSpace(trailText)
            ? "The Guardian returned a matching film review article."
            : trailText.Trim();

        return $"{baseSnippet} This result was selected for '{movieTitle}' within the {releaseYear}-{releaseYear + 1} film-review date window.";
    }
}
