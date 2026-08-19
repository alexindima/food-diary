using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Wearables;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class WearableClientTests {
    [Fact]
    public void FitbitGetAuthorizationUrl_ContainsExpectedOAuthParameters() {
        FitbitClient client = CreateFitbitClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        string url = client.GetAuthorizationUrl("state value");

        Assert.Equal(WearableProvider.Fitbit, client.Provider);
        Assert.Contains("client_id=fitbit-client", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fapp.test%2Ffitbit", url, StringComparison.Ordinal);
        Assert.Contains("scope=activity+heartrate+sleep", url, StringComparison.Ordinal);
        Assert.Contains("state=state%20value", url, StringComparison.Ordinal);
    }

    [Fact]
    public void FitbitGetAuthorizationUrl_EscapesClientIdentifier() {
        FitbitClient client = CreateFitbitClient(
            new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)),
            clientId: "client id&admin=true");

        string url = client.GetAuthorizationUrl("state");

        Assert.Contains("client_id=client%20id%26admin%3Dtrue", url, StringComparison.Ordinal);
        Assert.DoesNotContain("&admin=true", url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenClientIdMissing_ReturnsNullWithoutRequest() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        FitbitClient client = CreateFitbitClient(handler, clientId: "");

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenTokenResponseIsNull_ReturnsNull() {
        FitbitClient client = CreateFitbitClient(new RecordingHttpMessageHandler(_ => JsonResponse("null")));

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("{\"access_token\":\"\",\"refresh_token\":\"refresh\",\"user_id\":\"fitbit-user\",\"expires_in\":3600}")]
    [InlineData("{\"access_token\":\"access\",\"refresh_token\":\"\",\"user_id\":\"fitbit-user\",\"expires_in\":3600}")]
    [InlineData("{\"access_token\":\"access\",\"refresh_token\":\"refresh\",\"user_id\":\"\",\"expires_in\":3600}")]
    [InlineData("{\"access_token\":\"access\",\"refresh_token\":\"refresh\",\"user_id\":\"fitbit-user\",\"expires_in\":0}")]
    public async Task FitbitExchangeCodeAsync_WhenTokenResponseIsInvalid_ReturnsNull(string responseJson) {
        FitbitClient client = CreateFitbitClient(new RecordingHttpMessageHandler(_ => JsonResponse(responseJson)));

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenTokenRequestFails_ReturnsNull() {
        FitbitClient client = CreateFitbitClient(
            new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)));

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenCallerCancels_PropagatesCancellation() {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        FitbitClient client = CreateFitbitClient(new CanceledHttpMessageHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.ExchangeCodeAsync("code", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WithValidResponse_ReturnsToken() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            return JsonResponse("""{"access_token":"access","refresh_token":"refresh","user_id":"fitbit-user","expires_in":3600}""");
        });
        FitbitClient client = CreateFitbitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Multiple(
            () => Assert.Equal("access", result.AccessToken),
            () => Assert.Equal("refresh", result.RefreshToken),
            () => Assert.Equal("fitbit-user", result.ExternalUserId),
            () => Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc));
    }

    [Fact]
    public async Task FitbitRefreshTokenAsync_WithValidResponse_ReturnsToken() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
            return JsonResponse("""{"access_token":"access-next","refresh_token":"refresh-next","user_id":"fitbit-user","expires_in":3600}""");
        });
        FitbitClient client = CreateFitbitClient(handler);

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Multiple(
            () => Assert.Equal("access-next", result.AccessToken),
            () => Assert.Equal("refresh-next", result.RefreshToken),
            () => Assert.Equal("fitbit-user", result.ExternalUserId),
            () => Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc));
    }

    [Fact]
    public async Task FitbitRefreshTokenAsync_WhenTokenResponseIsNull_ReturnsNull() {
        FitbitClient client = CreateFitbitClient(new RecordingHttpMessageHandler(_ => JsonResponse("null")));

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitRefreshTokenAsync_WhenRequestFails_ReturnsNull() {
        FitbitClient client = CreateFitbitClient(
            new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)));

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WithDailyResponses_MapsDataPoints() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            string url = request.RequestUri!.AbsoluteUri;
            if (url.Contains("/activities/date/", StringComparison.Ordinal)) {
                return JsonResponse("""{"summary":{"steps":1000,"caloriesOut":500,"veryActiveMinutes":10,"fairlyActiveMinutes":15}}""");
            }

            if (url.Contains("/activities/heart/date/", StringComparison.Ordinal)) {
                return JsonResponse("""{"activities-heart":[{"value":{"restingHeartRate":58}}]}""");
            }

            return JsonResponse("""{"summary":{"totalMinutesAsleep":420}}""");
        });
        FitbitClient client = CreateFitbitClient(handler);

        Result<IReadOnlyList<WearableDataPoint>> result = await client.FetchDailyDataAsync(
            "access",
            new DateTime(2026, 4, 6),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            point => Assert.Equal((WearableDataType.Steps, 1000d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.CaloriesBurned, 500d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.ActiveMinutes, 25d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.HeartRate, 58d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.SleepMinutes, 420d), (point.DataType, point.Value)));
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenRequestFails_ReturnsFailure() {
        FitbitClient client = CreateFitbitClient(
            new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        Result<IReadOnlyList<WearableDataPoint>> result = await client.FetchDailyDataAsync(
            "access",
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wearable.SyncFailed", result.Error.Code);
        Assert.Equal(ErrorKind.ExternalFailure, result.Error.Kind);
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenLaterRequestFails_DiscardsPartialData() {
        var handler = new RecordingHttpMessageHandler(
            _ => JsonResponse("""{"summary":{"steps":1000,"caloriesOut":500}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        FitbitClient client = CreateFitbitClient(handler);

        Result<IReadOnlyList<WearableDataPoint>> result = await client.FetchDailyDataAsync(
            "access",
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wearable.SyncFailed", result.Error.Code);
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenPayloadHasUnexpectedValueType_ReturnsFailure() {
        FitbitClient client = CreateFitbitClient(
            new RecordingHttpMessageHandler(
                _ => JsonResponse("""{"summary":{"steps":"not-a-number"}}""")));

        Result<IReadOnlyList<WearableDataPoint>> result = await client.FetchDailyDataAsync(
            "access",
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wearable.SyncFailed", result.Error.Code);
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenCompositeDeadlineExpires_ReturnsFailure() {
        var handler = new BlockingHttpMessageHandler();
        FitbitClient client = CreateFitbitClient(handler, dailyDataOperationTimeout: TimeSpan.FromMilliseconds(50));

        Result<IReadOnlyList<WearableDataPoint>> result = await client.FetchDailyDataAsync(
            "access",
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wearable.SyncFailed", result.Error.Code);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenCallerCancels_PropagatesCancellation() {
        var handler = new BlockingHttpMessageHandler();
        FitbitClient client = CreateFitbitClient(handler, dailyDataOperationTimeout: TimeSpan.FromSeconds(5));
        using var cancellationTokenSource = new CancellationTokenSource();

        Task<Result<IReadOnlyList<WearableDataPoint>>> pending = client.FetchDailyDataAsync(
            "access",
            DateTime.UtcNow,
            cancellationTokenSource.Token);
        await handler.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), TimeProvider.System);
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    private static FitbitClient CreateFitbitClient(
        HttpMessageHandler handler,
        string clientId = "fitbit-client",
        TimeSpan? dailyDataOperationTimeout = null) {
        return new FitbitClient(
            new HttpClient(handler),
            MsOptions.Create(new FitbitOptions {
                ClientId = clientId,
                ClientSecret = "fitbit-secret",
                RedirectUri = "https://app.test/fitbit",
            }),
            FixedTime,
            NullLogger<FitbitClient>.Instance) {
            DailyDataOperationTimeout = dailyDataOperationTimeout ?? TimeSpan.FromSeconds(30),
        };
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders) : HttpMessageHandler {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new(responders);
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            Func<HttpRequestMessage, HttpResponseMessage> responder = _responders.Count > 1
                ? _responders.Dequeue()
                : _responders.Peek();
            return Task.FromResult(responder(request));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CanceledHttpMessageHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class BlockingHttpMessageHandler : HttpMessageHandler {
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestCount++;
            RequestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, TimeProvider.System, cancellationToken);
            throw new System.Diagnostics.UnreachableException();
        }
    }
}
