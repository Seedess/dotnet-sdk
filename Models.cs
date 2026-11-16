namespace Seedess;

public sealed record Playback(
    string? Format,
    string? Master,
    string? Manifest,
    string? Playlist,
    string Signaling,
    string? Config);

public sealed record Stream(
    string Id,
    string? Title,
    bool? Live,
    string? Status,
    string? Format,
    bool? KillSwitch,
    double? TargetDuration,
    int? MediaSequence,
    List<Dictionary<string, object?>>? Variants,
    int? Segments,
    string? UpdatedAt,
    string? PublicToken,
    Playback? Playback);

public sealed record Metrics(
    long CdnBytes,
    long P2pBytes,
    double OffloadPercent,
    long HashFailures,
    long Fallbacks,
    long Rebuffers,
    long PeakPeers,
    long Sessions);

public sealed record CreateStreamInput(string Url, string? Id = null, string? Title = null, bool? Live = null);

public sealed record TelemetryInput(
    string SessionId,
    string StreamId,
    long? CdnBytes = null,
    long? P2pBytes = null,
    long? HashFailures = null,
    long? Fallbacks = null,
    long? Rebuffers = null,
    long? PeakPeers = null);
