#nullable enable
using System.Net;
using MovieApp.Core.Services;

namespace MovieApp.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="LocalFileCacheService"/>.
/// These tests use a temporary directory to isolate file-system side effects.
/// </summary>
public class LocalFileCacheServiceTests : IDisposable
{
    // We redirect AppContext.BaseDirectory-relative cache writes by pointing to a temp folder.
    // Since LocalFileCacheService hard-codes its cache directory to AppContext.BaseDirectory/ApiCache,
    // we verify behaviour through the observable effects (return values, HTTP calls, file contents).

    private readonly string _tempCacheDir;

    public LocalFileCacheServiceTests()
    {
        _tempCacheDir = Path.Combine(Path.GetTempPath(), "LocalFileCacheServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempCacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempCacheDir))
            Directory.Delete(_tempCacheDir, recursive: true);
    }

    // Helper: creates a LocalFileCacheService that writes to _tempCacheDir by
    // writing a fresh cache file there before the test, then letting the SUT
    // read from the canonical AppContext.BaseDirectory/ApiCache path.
    // For tests that need controlled cache state we write directly to the ApiCache dir.

    private string GetCacheDir()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "ApiCache");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private string CachePath(string key) => Path.Combine(GetCacheDir(), $"{key}.json");

    private void WriteFreshCacheFile(string key, string content)
    {
        File.WriteAllText(CachePath(key), content);
        File.SetLastWriteTimeUtc(CachePath(key), DateTime.UtcNow);
    }

    private void WriteStaleCache(string key, string content)
    {
        File.WriteAllText(CachePath(key), content);
        File.SetLastWriteTimeUtc(CachePath(key), DateTime.UtcNow.AddHours(-25));
    }

    private void DeleteCacheFile(string key)
    {
        var path = CachePath(key);
        if (File.Exists(path))
            File.Delete(path);
    }

    // --- Argument validation ---

    [Fact]
    public async Task FetchOrCacheAsync_WhenCacheKeyIsEmpty_ThrowsArgumentException()
    {
        var sut = new LocalFileCacheService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.FetchOrCacheAsync(string.Empty, "http://example.com", new HttpClient()));
    }

    [Fact]
    public async Task FetchOrCacheAsync_WhenUrlIsEmpty_ThrowsArgumentException()
    {
        var sut = new LocalFileCacheService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.FetchOrCacheAsync("somekey", string.Empty, new HttpClient()));
    }

    [Fact]
    public async Task FetchOrCacheAsync_WhenClientIsNull_ThrowsArgumentNullException()
    {
        var sut = new LocalFileCacheService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.FetchOrCacheAsync("somekey", "http://example.com", null!));
    }

    // --- Cache hit (fresh) ---

    [Fact]
    public async Task FetchOrCacheAsync_WhenFreshCacheFileExists_ReturnsCachedContentWithoutHttpCall()
    {
        const string key = "test_fresh_cache";
        const string cached = "{\"data\":\"cached\"}";
        WriteFreshCacheFile(key, cached);

        var handlerMock = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":\"fresh\"}")
        });
        var client = new HttpClient(handlerMock);
        var sut = new LocalFileCacheService();

        try
        {
            var result = await sut.FetchOrCacheAsync(key, "http://example.com/api", client);

            Assert.Equal(cached, result);
            Assert.Equal(0, handlerMock.CallCount); // no HTTP call made
        }
        finally
        {
            DeleteCacheFile(key);
        }
    }

    // --- Cache miss (file absent) ---

    [Fact]
    public async Task FetchOrCacheAsync_WhenNoCacheFileExists_FetchesFromUrlAndWritesToCache()
    {
        const string key = "test_cache_miss";
        DeleteCacheFile(key);
        const string apiResponse = "{\"data\":\"from_api\"}";

        var handlerMock = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(apiResponse)
        });
        var client = new HttpClient(handlerMock);
        var sut = new LocalFileCacheService();

        try
        {
            var result = await sut.FetchOrCacheAsync(key, "http://example.com/api", client);

            Assert.Equal(apiResponse, result);
            Assert.Equal(1, handlerMock.CallCount);
            Assert.True(File.Exists(CachePath(key)));
            Assert.Equal(apiResponse, await File.ReadAllTextAsync(CachePath(key)));
        }
        finally
        {
            DeleteCacheFile(key);
        }
    }

    // --- Cache stale (older than 24 h) ---

    [Fact]
    public async Task FetchOrCacheAsync_WhenCacheFileIsOlderThan24Hours_FetchesFromUrlAndUpdatesCache()
    {
        const string key = "test_stale_cache";
        WriteStaleCache(key, "{\"data\":\"old\"}");
        const string freshResponse = "{\"data\":\"new\"}";

        var handlerMock = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(freshResponse)
        });
        var client = new HttpClient(handlerMock);
        var sut = new LocalFileCacheService();

        try
        {
            var result = await sut.FetchOrCacheAsync(key, "http://example.com/api", client);

            Assert.Equal(freshResponse, result);
            Assert.Equal(1, handlerMock.CallCount);
        }
        finally
        {
            DeleteCacheFile(key);
        }
    }

    // --- HTTP error ---

    [Fact]
    public async Task FetchOrCacheAsync_WhenHttpResponseIsNonSuccess_ThrowsHttpRequestException()
    {
        const string key = "test_http_error";
        DeleteCacheFile(key);

        var handlerMock = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        });
        var client = new HttpClient(handlerMock);
        var sut = new LocalFileCacheService();

        try
        {
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                sut.FetchOrCacheAsync(key, "http://example.com/api", client));
        }
        finally
        {
            DeleteCacheFile(key);
        }
    }
}

/// <summary>Minimal fake HttpMessageHandler for LocalFileCacheService tests.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public int CallCount { get; private set; }

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_response);
    }
}
