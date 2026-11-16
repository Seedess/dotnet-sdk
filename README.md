# Seedess.Sdk

Official Seedess SDK for .NET.

**Server-side only.** The SDK uses a secret API key (`key_…`) to create and control streams.
Never ship it in a browser or a mobile app. Playback uses the separate `@seedess/*-p2p`
packages and a per-stream public token (`pk_…`).

## Install

```bash
dotnet add package Seedess.Sdk
```

.NET 8+ (built for .NET 10 in this repository).

## Use

```csharp
using Seedess;

var seedess = new SeedessClient(Environment.GetEnvironmentVariable("SEEDESS_API_KEY")!);

var stream = await seedess.CreateStreamAsync(new CreateStreamInput(
    "https://origin.example/master.m3u8",
    Id: "demo",
    Live: true));

Console.WriteLine(stream.PublicToken);
Console.WriteLine(stream.Playback?.Master);
```

Pass only the public token and stream id to the page.

## Methods

| Method | HTTP | Purpose |
| --- | --- | --- |
| `CreateStreamAsync(CreateStreamInput)` | `POST /api/v1/streams` | Ingest a manifest, mint the public token. |
| `GetStreamAsync(id)` | `GET /api/v1/streams/:id` | Status, variants, segment count. |
| `ListStreamsAsync()` | `GET /api/v1/streams` | All streams in the tenant. |
| `RefreshStreamAsync(id)` | `POST /api/v1/streams/:id/refresh` | Pull new live segments. |
| `DisablePeersAsync(id)` | `POST /api/v1/streams/:id/kill` | CDN only; playback continues. |
| `EnablePeersAsync(id)` | `POST /api/v1/streams/:id/revive` | Re-enable peer delivery. |
| `GetMetricsAsync(id)` | `GET /api/v1/streams/:id/metrics` | Offload and delivery counters. |
| `SendTelemetryAsync(TelemetryInput)` | `POST /api/v1/telemetry` | One session batch (public, rate limited). |

`RequestAsync(method, path, body, auth)` returns the raw `JsonElement`.

## Errors

```csharp
try
{
    await seedess.CreateStreamAsync(new CreateStreamInput(url));
}
catch (SeedessException error)
{
    Console.WriteLine($"{error.Status} {error.ErrorCode} {error.Message}");
}
```

## Links

- Docs: https://seedess.com/docs/
- API: https://seedess.com/docs/api/
- Libraries: https://seedess.com/docs/libraries/

## License

MIT.
