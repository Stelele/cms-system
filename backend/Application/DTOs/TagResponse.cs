using System.Text.Json.Serialization;

namespace Application.DTOs;

public record TagResponse(
    string Tag,
    int Count
);
