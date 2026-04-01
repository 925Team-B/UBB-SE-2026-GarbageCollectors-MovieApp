#nullable enable

using MovieApp.Core.Interfaces;

namespace MovieApp.Core.Services;

public sealed class LocalFileCacheService : ICacheService
{
    private readonly string _cacheDirectory;

    public LocalFileCacheService()
    {
        _cacheDirectory = Path.Combine(AppContext.BaseDirectory, "ApiCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string> FetchOrCacheAsync(string cacheKey, string url, HttpClient client)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            throw new ArgumentException("Cache key is required.", nameof(cacheKey));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required.", nameof(url));

        ArgumentNullException.ThrowIfNull(client);

        var cachePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");

        if (File.Exists(cachePath))
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
            if (age < TimeSpan.FromHours(24))
                return await File.ReadAllTextAsync(cachePath);
        }

        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync(cachePath, json);
        return json;
    }
}
