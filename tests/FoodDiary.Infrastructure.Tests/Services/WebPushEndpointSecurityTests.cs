using System.Net;
using FoodDiary.Integrations.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushEndpointSecurityTests {
    [Theory]
    [InlineData("http://push.example.com/subscription")]
    [InlineData("https://127.0.0.1/subscription")]
    [InlineData("https://push.example.com:8443/subscription")]
    [InlineData("https://user@push.example.com/subscription")]
    public async Task ValidationHandler_WhenEndpointIsUnsafe_RejectsBeforeTransport(string endpoint) {
        var transport = new RecordingHandler();
        using var handler = new WebPushEndpointValidationHandler {
            InnerHandler = transport,
        };
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync(
            endpoint,
            new ByteArrayContent([]),
            CancellationToken.None));

        Assert.Equal(0, transport.SendCount);
    }

    [Fact]
    public async Task ValidationHandler_WhenEndpointIsPublicHttps_AllowsTransport() {
        var transport = new RecordingHandler();
        using var handler = new WebPushEndpointValidationHandler {
            InnerHandler = transport,
        };
        using var client = new HttpClient(handler);

        using HttpResponseMessage response = await client.PostAsync(
            "https://push.example.com/subscription?token=test",
            new ByteArrayContent([]),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, transport.SendCount);
    }

    [Fact]
    public async Task PrimaryHandler_WhenHostnameResolvesToLoopback_RejectsConnection() {
        using var validationHandler = new WebPushEndpointValidationHandler {
            InnerHandler = WebPushSocketsHttpHandlerFactory.Create(),
        };
        using var client = new HttpClient(validationHandler);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync(
            "https://localhost/subscription",
            new ByteArrayContent([]),
            CancellationToken.None));

        Assert.Contains("non-public network address", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("10.0.0.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("169.254.1.1", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.0.1", false)]
    [InlineData("::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("::10.0.0.1", false)]
    [InlineData("::ffff:8.8.8.8", true)]
    [InlineData("::ffff:127.0.0.1", false)]
    [InlineData("64:ff9b::7f00:1", false)]
    [InlineData("2002:7f00:1::", false)]
    [InlineData("2001:4860:4860::8888", true)]
    public void IsPubliclyRoutable_ClassifiesAddress(string value, bool expected) {
        Assert.Equal(expected, WebPushSocketsHttpHandlerFactory.IsPubliclyRoutable(IPAddress.Parse(value)));
    }

    [Fact]
    public async Task ConnectToAddressesAsync_WhenFirstAddressFails_UsesNextAddress() {
        IPAddress[] addresses = [IPAddress.Parse("8.8.8.8"), IPAddress.Parse("1.1.1.1")];
        var attemptedEndpoints = new List<IPEndPoint>();
        var expectedStream = new MemoryStream();

        Stream stream = await WebPushSocketsHttpHandlerFactory.ConnectToAddressesAsync(
            addresses,
            443,
            (endpoint, _) => {
                attemptedEndpoints.Add(endpoint);
                return attemptedEndpoints.Count == 1
                    ? ValueTask.FromException<Stream>(new IOException("first address failed"))
                    : ValueTask.FromResult<Stream>(expectedStream);
            },
            CancellationToken.None);

        Assert.Same(expectedStream, stream);
        Assert.Equal(addresses, attemptedEndpoints.Select(static endpoint => endpoint.Address));
        Assert.All(attemptedEndpoints, static endpoint => Assert.Equal(443, endpoint.Port));
    }

    [Fact]
    public async Task ConnectToAddressesAsync_WhenAllAddressesFail_ReportsLastFailure() {
        IPAddress[] addresses = [IPAddress.Parse("8.8.8.8"), IPAddress.Parse("1.1.1.1")];
        var expected = new IOException("connection failed");
        int attempts = 0;

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await WebPushSocketsHttpHandlerFactory.ConnectToAddressesAsync(
                addresses,
                443,
                (_, _) => {
                    attempts++;
                    return ValueTask.FromException<Stream>(expected);
                },
                CancellationToken.None));

        Assert.Equal(addresses.Length, attempts);
        Assert.Same(expected, exception.InnerException);
    }

    [Fact]
    public async Task ConnectToAddressesAsync_WhenCallerCancels_DoesNotAttemptConnection() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        int attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await WebPushSocketsHttpHandlerFactory.ConnectToAddressesAsync(
                [IPAddress.Parse("8.8.8.8")],
                443,
                (_, _) => {
                    attempts++;
                    return ValueTask.FromResult<Stream>(new MemoryStream());
                },
                cancellationTokenSource.Token));

        Assert.Equal(0, attempts);
    }

    [Fact]
    public async Task ConnectToAddressesAsync_WhenConnectorCancels_PropagatesCancellation() {
        var expected = new OperationCanceledException("connector canceled");

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await WebPushSocketsHttpHandlerFactory.ConnectToAddressesAsync(
                [IPAddress.Parse("8.8.8.8")],
                443,
                (_, _) => ValueTask.FromException<Stream>(expected),
                CancellationToken.None));

        Assert.Same(expected, exception);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler : HttpMessageHandler {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
