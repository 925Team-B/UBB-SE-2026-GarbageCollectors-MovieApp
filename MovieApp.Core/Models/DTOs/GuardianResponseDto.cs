#nullable enable
using System.Text.Json.Serialization;

namespace MovieApp.Core.Models.DTOs;

public sealed class GuardianApiResponseDto
{
    [JsonPropertyName("response")]
    public GuardianResponseDto? Response { get; set; }
}

public sealed class GuardianResponseDto
{
    [JsonPropertyName("results")]
    public List<GuardianResultDto> Results { get; set; } = new ();
}

public sealed class GuardianResultDto
{
    [JsonPropertyName("webTitle")]
    public string WebTitle { get; set; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public GuardianFieldsDto? Fields { get; set; }
}

public sealed class GuardianFieldsDto
{
    [JsonPropertyName("trailText")]
    public string TrailText { get; set; } = string.Empty;
}
