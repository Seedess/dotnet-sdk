# .NET SDK example

A complete server + browser sample:

- `Program.cs` — creates a stream with `Seedess.Sdk`, then serves the page and `/api/seedess`.
- `public/index.html` — loads hls.js and `https://seedess.com/p2p.js`, then plays the stream
  with the P2P adapter.

## Run

```bash
cd sdks/dotnet-sdk
SEEDESS_API_KEY=key_… dotnet run --project examples/Seedess.Example.csproj
open http://localhost:8787
```

Optional: `SEEDESS_STREAM_URL`, `SEEDESS_STREAM_ID`, `SEEDESS_API_BASE`, `PORT`.

The first request to `/api/seedess` creates the stream. The API key stays in the server
process; the browser only sees the public token and playback URLs.
