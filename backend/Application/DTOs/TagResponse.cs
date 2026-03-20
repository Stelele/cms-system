using System.Text.Json.Serialization;

namespace Application.DTOs;

public class TagResponse
{
    [JsonPropertyName("tag")]
    public string Tag { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
