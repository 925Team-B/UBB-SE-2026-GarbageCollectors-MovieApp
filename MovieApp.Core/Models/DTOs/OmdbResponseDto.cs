#nullable enable
using System.Text.Json.Serialization;

namespace MovieApp.Core.Models.DTOs;

public sealed class OmdbResponseDto
{
    [JsonPropertyName("Ratings")]
    public List<OmdbRatingDto> Ratings { get; set; } = new();
}

public sealed class OmdbRatingDto
{
    [JsonPropertyName("Source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("Value")]
    public string Value { get; set; } = string.Empty;
}
