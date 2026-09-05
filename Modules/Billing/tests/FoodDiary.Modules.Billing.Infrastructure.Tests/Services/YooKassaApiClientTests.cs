using System.Net;
using System.Text;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class YooKassaApiClientTests {
    [Fact]
    public async Task SendAsync_WithPost_BuildsAuthenticatedRequestAndPreservesIdempotenceKey() {
        RecordingHandler handler = new(_ => JsonResponse("""{ "value": "created" }"""));
        YooKassaApiClient client = CreateClient(handler);

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Post,
            "payments",
            new { amount = 299 },
            " request-key ",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Multiple(
            () => Assert.Equal("created", result.Value.Value),
            () => Assert.Equal(new Uri("https://api.yookassa.test/v3/payments"), handler.RequestUri),
            () => Assert.Equal("Basic", handler.AuthorizationScheme),
            () => Assert.Equal("c2hvcDpzZWNyZXQ=", handler.AuthorizationParameter),
            () => Assert.Equal("request-key", handler.IdempotenceKey),
            () => Assert.Contains("\"amount\":299", handler.Body, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_WithGet_DoesNotAddIdempotenceKeyOrBody() {
        RecordingHandler handler = new(_ => JsonResponse("""{ "value": "found" }"""));
        YooKassaApiClient client = CreateClient(handler);

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Get,
            "payments/payment-1",
            body: null,
            idempotenceKey: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Multiple(
            () => Assert.Null(handler.IdempotenceKey),
            () => Assert.Null(handler.Body));
    }

    [Fact]
    public async Task SendAsync_WithBlankIdempotenceKey_GeneratesKey() {
        RecordingHandler handler = new(_ => JsonResponse("""{ "value": "created" }"""));
        YooKassaApiClient client = CreateClient(handler);

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Post,
            "payments",
            body: null,
            idempotenceKey: " ",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(Guid.TryParseExact(handler.IdempotenceKey, "N", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_WhenProviderReturnsError_ReturnsSafeFailure(HttpStatusCode statusCode) {
        YooKassaApiClient client = CreateClient(new RecordingHandler(_ => new HttpResponseMessage(statusCode)));

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Get,
            "payments/payment-1",
            body: null,
            idempotenceKey: null,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.Contains("YooKassa", result.Error.Message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{ invalid")]
    public async Task SendAsync_WhenResponseIsInvalid_ReturnsSafeFailure(string json) {
        YooKassaApiClient client = CreateClient(new RecordingHandler(_ => JsonResponse(json)));

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Get,
            "payments/payment-1",
            body: null,
            idempotenceKey: null,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.Contains("YooKassa", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_WhenTransportFails_ReturnsSafeFailure() {
        YooKassaApiClient client = CreateClient(new RecordingHandler(_ => throw new HttpRequestException("secret transport detail")));

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Get,
            "payments/payment-1",
            body: null,
            idempotenceKey: null,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.DoesNotContain("secret transport detail", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_WhenRequestTimesOutWithoutCallerCancellation_ReturnsSafeTimeoutFailure() {
        YooKassaApiClient client = CreateClient(new RecordingHandler(_ => throw new OperationCanceledException("internal http timeout")));

        Result<TestResponse> result = await client.SendAsync<TestResponse>(
            HttpMethod.Get,
            "payments/payment-1",
            body: null,
            idempotenceKey: null,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.Contains("timed out", result.Error.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenCallerCancels_PropagatesCancellation() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        YooKassaApiClient client = CreateClient(new RecordingHandler(_ => throw new TaskCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendAsync<TestResponse>(
                HttpMethod.Get,
                "payments/payment-1",
                body: null,
                idempotenceKey: null,
                cancellation.Token));
    }

    private static YooKassaApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            new YooKassaOptions {
                ShopId = "shop",
                SecretKey = "secret",
                ApiBaseUrl = "https://api.yookassa.test/v3",
            });

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [ExcludeFromCodeCoverage]
    private sealed record TestResponse(string Value);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? IdempotenceKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            IdempotenceKey = request.Headers.TryGetValues("Idempotence-Key", out IEnumerable<string>? values)
                ? Assert.Single(values)
                : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}
