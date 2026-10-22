using System.Text.Json.Serialization;

namespace management.api.Contracts;

/// <summary>
/// Файл набора. upload_url есть только пока файл в состоянии uploading и больше никуда не используется.
/// </summary>
public sealed class FileDescription
{
    public Guid FileId { get; init; }

    public required string Name { get; init; }

    public long Size { get; init; }

    public required string State { get; init; }

    public string? Error { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UploadUrl { get; init; }
}
