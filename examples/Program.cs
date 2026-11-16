using Seedess;

const string Origin = "https://seedess.com";

var apiKey = Environment.GetEnvironmentVariable("SEEDESS_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set SEEDESS_API_KEY (a server key, key_...) before running this example.");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8787";
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
var app = builder.Build();

var client = new SeedessClient(
    apiKey,
    Environment.GetEnvironmentVariable("SEEDESS_API_BASE") ?? Origin);

var streamUrl = Environment.GetEnvironmentVariable("SEEDESS_STREAM_URL")
    ?? "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8";
var streamId = Environment.GetEnvironmentVariable("SEEDESS_STREAM_ID") ?? "sdk-example";

Dictionary<string, object?>? cached = null;
var gate = new object();

app.MapGet("/api/seedess", async () =>
{
    lock (gate) { if (cached is not null) return Results.Json(cached); }

    var stream = await client.CreateStreamAsync(new CreateStreamInput(
        streamUrl, streamId, "Seedess .NET SDK example", null));
    var token = stream.PublicToken ?? string.Empty;
    var payload = new Dictionary<string, object?>
    {
        ["ok"] = true,
        ["stream"] = stream.Id,
        ["token"] = token,
        ["playback"] = new Dictionary<string, string>
        {
            ["master"] = Origin + stream.Playback?.Master,
            ["signaling"] = Origin + stream.Playback?.Signaling,
            ["config"] = $"{Origin}/signaling/config?token={Uri.EscapeDataString(token)}",
        },
    };
    lock (gate) { cached = payload; }
    return Results.Json(payload);
});

app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(AppContext.BaseDirectory, "public", "index.html"));
});

Console.WriteLine($"Seedess example on http://localhost:{port}");
Console.WriteLine("The first /api/seedess request creates the stream.");
app.Run();
return 0;
