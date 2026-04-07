#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using MovieApp.Core.Interfaces;
using MovieApp.Core.Models;
using MovieApp.Core.Models.DTOs;

namespace MovieApp.Core.Services;

public sealed class NytReviewProvider : IExternalReviewProvider
{
    private readonly HttpClient httpClient;
    private readonly ICacheService cacheService;

    public NytReviewProvider(HttpClient httpClient, ICacheService cacheService)
    {
        this.httpClient = httpClient;
        this.cacheService = cacheService;
    }

    public async Task<CriticReview?> GetReviewAsync(string movieTitle, int releaseYear)
    {
        if (string.IsNullOrWhiteSpace(movieTitle))
        {
            return null;
        }

        var (canonicalTitle, canonicalYear, canonicalDirector) = await GetOmdbContextAsync(movieTitle, releaseYear);

        var queryPhrases = new List<string>
        {
            $"\"{canonicalTitle}\"",
            $"\"{canonicalYear}\""
        };

        if (!string.IsNullOrWhiteSpace(canonicalDirector))
        {
            queryPhrases.Add($"\"{canonicalDirector}\"");
        }

        var query = Uri.EscapeDataString(string.Join(' ', queryPhrases));
        var fullFilterQuery = Uri.EscapeDataString("type_of_material:(\"Review\") AND subject:(\"Movies\")");

        var searchVariants = new[]
        {
            ($"https://api.nytimes.com/svc/search/v2/articlesearch.json?q={query}&fq={fullFilterQuery}&sort=relevance&api-key=50k6GUkhjA7OiKdLuL11ucYiyffwBj4j640MCDVBdeQu9UXl", BuildCacheKey("nyt_full", movieTitle, releaseYear)),
            ($"https://api.nytimes.com/svc/search/v2/articlesearch.json?q={Uri.EscapeDataString($"\"{canonicalTitle}\"")}&fq={fullFilterQuery}&sort=relevance&api-key=50k6GUkhjA7OiKdLuL11ucYiyffwBj4j640MCDVBdeQu9UXl", BuildCacheKey("nyt_title_only", movieTitle, releaseYear)),
            ($"https://api.nytimes.com/svc/search/v2/articlesearch.json?q={Uri.EscapeDataString(canonicalTitle)}&fq={Uri.EscapeDataString("type_of_material:(\"Review\")")}&sort=relevance&api-key=50k6GUkhjA7OiKdLuL11ucYiyffwBj4j640MCDVBdeQu9UXl", BuildCacheKey("nyt_review_fallback", movieTitle, releaseYear))
        };

        NytApiResponseDto? dto = null;

        foreach (var (url, cacheKey) in searchVariants)
        {
            var json = await cacheService.FetchOrCacheAsync(cacheKey, url, httpClient);
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            try
            {
                dto = JsonSerializer.Deserialize<NytApiResponseDto>(json);
            }
            catch (JsonException)
            {
                continue;
            }

            if (dto?.Response?.Docs?.Any() == true)
            {
                break;
            }
        }

        if (dto?.Response?.Docs?.Any() != true)
        {
            return null;
        }

        var doc = dto?.Response?.Docs?
            .Where(d => IsSpecificMovieReview(canonicalTitle, canonicalYear, d.Headline?.Main, d.Snippet))
            .OrderByDescending(d => MatchScore(movieTitle, releaseYear, d.Headline?.Main, d.Snippet))
            .FirstOrDefault();
        if (doc is null)
        {
            return null;
        }

        return new CriticReview
        {
            Source = "New York Times",
            Score = 0,
            Headline = doc.Headline?.Main ?? string.Empty,
            Snippet = BuildLongerSnippet(doc.Snippet, canonicalTitle, canonicalYear),
            Url = doc.WebUrl
        };
    }

    private async Task<(string Title, int Year, string Director)> GetOmdbContextAsync(string movieTitle, int releaseYear)
    {
        var omdbUrl = $"https://www.omdbapi.com/?apikey=57b3a80a&t={Uri.EscapeDataString(movieTitle)}&y={releaseYear}";
        var omdbCacheKey = BuildCacheKey("nyt_omdb_context", movieTitle, releaseYear);

        var json = await cacheService.FetchOrCacheAsync(omdbCacheKey, omdbUrl, httpClient);
        if (string.IsNullOrWhiteSpace(json))
        {
            return (movieTitle, releaseYear, string.Empty);
        }

        OmdbContextDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<OmdbContextDto>(json);
        }
        catch (JsonException)
        {
            return (movieTitle, releaseYear, string.Empty);
        }

        if (dto is null)
        {
            return (movieTitle, releaseYear, string.Empty);
        }
        var title = string.IsNullOrWhiteSpace(dto.Title) ? movieTitle : dto.Title;
        var year = int.TryParse(dto.Year, out var parsedYear) ? parsedYear : releaseYear;
        var director = string.IsNullOrWhiteSpace(dto.Director) ? string.Empty : dto.Director.Split(',', StringSplitOptions.TrimEntries)[0];

        return (title, year, director);
    }

    private static string BuildCacheKey(string provider, string movieTitle, int releaseYear)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(movieTitle.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return $"{provider}_{sanitized}_{releaseYear}_v3".Replace(' ', '_').ToLowerInvariant();
    }

    private static int MatchScore(string movieTitle, int releaseYear, string? headline, string? snippet)
    {
        var text = $"{headline} {snippet}";

        var score = 0;
        if (text.Contains(movieTitle, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }
        var tokens = movieTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToList();

        score += tokens.Count(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

        if (text.Contains(releaseYear.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        return score;
    }

    private static bool IsSpecificMovieReview(string movieTitle, int releaseYear, string? headline, string? snippet)
    {
        var combined = $"{headline} {snippet}";
        if (string.IsNullOrWhiteSpace(combined))
        {
            return false;
        }
        var hasFullTitle = combined.Contains(movieTitle, StringComparison.OrdinalIgnoreCase);
        var hasYear = combined.Contains(releaseYear.ToString(), StringComparison.OrdinalIgnoreCase);
        var hasReviewCue = combined.Contains("review", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("film", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("movie", StringComparison.OrdinalIgnoreCase);

        var looksGenericRoundup = combined.Contains("best movies", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("top movies", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("movies of", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("ranked", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("streaming", StringComparison.OrdinalIgnoreCase);

        if (looksGenericRoundup && !hasFullTitle)
        {
            return false;
        }

        var titleTokens = movieTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToList();
        var tokenMatches = titleTokens.Count(t => combined.Contains(t, StringComparison.OrdinalIgnoreCase));

        return hasFullTitle || tokenMatches >= Math.Max(1, titleTokens.Count / 2) || (hasYear && hasReviewCue);
    }

    private static string BuildLongerSnippet(string? snippet, string canonicalTitle, int canonicalYear)
    {
        var baseSnippet = string.IsNullOrWhiteSpace(snippet)
            ? "New York Times returned a matching movie review article."
            : snippet.Trim();

        return $"{baseSnippet} This match was selected for '{canonicalTitle}' ({canonicalYear}) from NYT movie-review search results.";
    }

    private sealed class OmdbContextDto
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("Year")]
        public string Year { get; set; } = string.Empty;

        [JsonPropertyName("Director")]
        public string Director { get; set; } = string.Empty;
    }
}