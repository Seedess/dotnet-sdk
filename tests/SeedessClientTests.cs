using System.Net;
using System.Text;
using Seedess;
using Xunit;

public sealed class FakeHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }

    public FakeHandler(HttpStatusCode status, string body)
    {
        _status = status;
        _body = body;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
    }
}

public class SeedessClientTests
{
    [Fact]
    public async Task CreateStreamSendsAuthAndBody()
    {
        var handler = new FakeHandler(HttpStatusCode.Created,
            "{\"ok\":true,\"id\":\"demo\",\"publicToken\":\"pk_1\",\"playback\":{\"signaling\":\"/s\"}}");
        var client = new SeedessClient("key_test", "https://seedess.test", new HttpClient(handler));

        var stream = await client.CreateStreamAsync(new CreateStreamInput(
            "https://origin/master.m3u8", "demo", null, true));

        Assert.Equal("demo", stream.Id);
        Assert.Equal("pk_1", stream.PublicToken);
        Assert.Equal("https://seedess.test/api/v1/streams", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("key_test", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains("\"id\":\"demo\"", handler.LastBody);
    }

    [Fact]
    public async Task DisablePeersMapsToKill()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, "{\"ok\":true}");
        var client = new SeedessClient("key_test", "https://seedess.test", new HttpClient(handler));
        await client.DisablePeersAsync("demo");
        Assert.EndsWith("/api/v1/streams/demo/kill", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task MetricsDeserialize()
    {
        var handler = new FakeHandler(HttpStatusCode.OK,
            "{\"ok\":true,\"id\":\"demo\",\"metrics\":{\"cdnBytes\":10,\"p2pBytes\":90,\"offloadPercent\":90}}");
        var client = new SeedessClient("key_test", "https://seedess.test", new HttpClient(handler));
        var metrics = await client.GetMetricsAsync("demo");
        Assert.Equal(90, metrics.P2pBytes);
        Assert.Equal(90, metrics.OffloadPercent);
    }

    [Fact]
    public async Task TelemetryHasNoApiKey()
    {
        var handler = new FakeHandler(HttpStatusCode.NoContent, "");
        var client = new SeedessClient("key_test", "https://seedess.test", new HttpClient(handler));
        await client.SendTelemetryAsync(new TelemetryInput("s", "demo"));
        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public void RejectsPublicToken()
    {
        Assert.Throws<ArgumentException>(() => new SeedessClient("pk_stream"));
    }
}
