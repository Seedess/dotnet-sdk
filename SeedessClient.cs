using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Seedess;

/// <summary>
/// Official Seedess SDK for .NET.
/// Server-side only: uses a secret API key (key_…). Playback uses the separate
/// @seedess/*-p2p packages and a per-stream public token.
/// </summary>
public sealed class SeedessClient
{
    public const string DefaultBaseUrl = "https://seedess.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly HttpClient _http;

    public SeedessClient(string apiKey, string? baseUrl = null, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("key_", StringComparison.Ordinal))
        {
            throw new ArgumentException("Seedess: apiKey must be a server API key (key_…)", nameof(apiKey));
        }
        _apiKey = apiKey;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>Raw JSON request. Returns the parsed JsonDocument root.</summary>
    public async Task<JsonElement> RequestAsync(
        HttpMethod method,
        string path,
        object? body = null,
        bool auth = true,
        CancellationToken cancellationToken = default)
    {
        var url = _baseUrl + (path.StartsWith('/') ? path : "/" + path);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (auth)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = string.IsNullOrWhiteSpace(raw)
            ? JsonDocument.Parse("null")
            : JsonDocument.Parse(raw);
        var root = document.RootElement.Clone();

        if (!response.IsSuccessStatusCode)
        {
            var code = "http_error";
            var message = $"HTTP {(int)response.StatusCode}";
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.Object)
            {
                if (error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    code = c.GetString() ?? code;
                }
                if (error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                {
                    message = m.GetString() ?? message;
                }
            }
            throw new SeedessException((int)response.StatusCode, code, message);
        }
        return root;
    }

    public async Task<Stream> CreateStreamAsync(CreateStreamInput input, CancellationToken cancellationToken = default)
    {
        var root = await RequestAsync(HttpMethod.Post, "/api/v1/streams", input, true, cancellationToken)
            .ConfigureAwait(false);
        return root.Deserialize<Stream>(JsonOptions)
            ?? throw new SeedessException(502, "invalid_response", "Seedess returned an empty stream");
    }

    public async Task<Stream> GetStreamAsync(string id, CancellationToken cancellationToken = default)
    {
        var root = await RequestAsync(HttpMethod.Get, $"/api/v1/streams/{Escape(id)}", null, true, cancellationToken)
            .ConfigureAwait(false);
        return root.Deserialize<Stream>(JsonOptions)
            ?? throw new SeedessException(502, "invalid_response", "Seedess returned an empty stream");
    }

    public async Task<IReadOnlyList<Stream>> ListStreamsAsync(CancellationToken cancellationToken = default)
    {
        var root = await RequestAsync(HttpMethod.Get, "/api/v1/streams", null, true, cancellationToken)
            .ConfigureAwait(false);
        if (!root.TryGetProperty("streams", out var streams) || streams.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<Stream>();
        }
        return streams.Deserialize<List<Stream>>(JsonOptions) ?? new List<Stream>();
    }

    public Task<JsonElement> RefreshStreamAsync(string id, CancellationToken cancellationToken = default) =>
        RequestAsync(HttpMethod.Post, $"/api/v1/streams/{Escape(id)}/refresh", null, true, cancellationToken);

    public Task<JsonElement> DisablePeersAsync(string id, CancellationToken cancellationToken = default) =>
        RequestAsync(HttpMethod.Post, $"/api/v1/streams/{Escape(id)}/kill", null, true, cancellationToken);

    public Task<JsonElement> EnablePeersAsync(string id, CancellationToken cancellationToken = default) =>
        RequestAsync(HttpMethod.Post, $"/api/v1/streams/{Escape(id)}/revive", null, true, cancellationToken);

    public async Task<Metrics> GetMetricsAsync(string id, CancellationToken cancellationToken = default)
    {
        var root = await RequestAsync(HttpMethod.Get, $"/api/v1/streams/{Escape(id)}/metrics", null, true, cancellationToken)
            .ConfigureAwait(false);
        if (!root.TryGetProperty("metrics", out var metrics))
        {
            throw new SeedessException(502, "invalid_response", "Seedess returned no metrics");
        }
        return metrics.Deserialize<Metrics>(JsonOptions)
            ?? throw new SeedessException(502, "invalid_response", "Seedess returned empty metrics");
    }

    public Task<JsonElement> SendTelemetryAsync(TelemetryInput input, CancellationToken cancellationToken = default) =>
        RequestAsync(HttpMethod.Post, "/api/v1/telemetry", input, false, cancellationToken);

    private static string Escape(string value) => Uri.EscapeDataString(value);
}
