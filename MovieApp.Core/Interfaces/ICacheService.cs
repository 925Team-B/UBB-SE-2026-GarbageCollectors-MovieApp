#nullable enable

using MovieApp;

namespace MovieApp.Core.Interfaces;

public interface ICacheService
{
    Task<string> FetchOrCacheAsync(string cacheKey, string url, HttpClient client);
}
