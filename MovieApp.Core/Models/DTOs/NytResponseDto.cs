#nullable enable
using System.Text.Json.Serialization;

namespace MovieApp.Core.Models.DTOs;

public sealed class NytApiResponseDto
{
    [JsonPropertyName("response")]
    public NytResponseDto? Response { get; set; }
}

public sealed class NytResponseDto
{
    [JsonPropertyName("docs")]
    public List<NytDocDto> Docs { get; set; } = new();
}

public sealed class NytDocDto
{
    [JsonPropertyName("headline")]
    public NytHeadlineDto? Headline { get; set; }

    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;
}

public sealed class NytHeadlineDto
{
    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;
}
