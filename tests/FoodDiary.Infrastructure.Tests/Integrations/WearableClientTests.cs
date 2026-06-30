using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Wearables;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class WearableClientTests {
    [Fact]
    public void GoogleFitGetAuthorizationUrl_ContainsExpectedOAuthParameters() {
        GoogleFitClient client = CreateGoogleFitClient(new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        string url = client.GetAuthorizationUrl("state value");

        Assert.Equal(WearableProvider.GoogleFit, client.Provider);
        Assert.Contains("client_id=google-client", url, StringComparison.Ordinal);
        Assert.Contains("redirect_uri=https%3A%2F%2Fapp.test%2Fgoogle", url, StringComparison.Ordinal);
        Assert.Contains("state=state%20value", url, StringComparison.Ordinal);
        Assert.Contains("access_type=offline", url, StringComparison.Ordinal);
        Assert.Contains("prompt=consent", url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GoogleFitExchangeCodeAsync_WhenClientIdMissing_ReturnsNullWithoutRequest() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        GoogleFitClient client = CreateGoogleFitClient(handler, clientId: "");

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GoogleFitExchangeCodeAsync_WhenTokenResponseIsNull_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("null"));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GoogleFitExchangeCodeAsync_WhenTokenRequestFails_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GoogleFitExchangeCodeAsync_WithValidResponses_ReturnsTokenAndExternalUserId() {
        var handler = new RecordingHttpMessageHandler(request => {
            if (string.Equals(request.RequestUri!.AbsoluteUri, "https://oauth2.googleapis.com/token", StringComparison.Ordinal)) {
                Assert.Equal(HttpMethod.Post, request.Method);
                return JsonResponse("""{"access_token":"access","refresh_token":"refresh","expires_in":3600}""");
            }

            Assert.Equal("https://www.googleapis.com/oauth2/v1/userinfo?access_token=access", request.RequestUri.AbsoluteUri);
            return JsonResponse("""{"id":"google-user"}""");
        });
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Equal("google-user", result.ExternalUserId);
        Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task GoogleFitRefreshTokenAsync_WhenRefreshTokenMissingInResponse_ReusesExistingRefreshToken() {
        var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse("""{"access_token":"access-next","expires_in":3600}"""));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.RefreshTokenAsync("existing-refresh", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("access-next", result.AccessToken);
        Assert.Equal("existing-refresh", result.RefreshToken);
        Assert.Equal(string.Empty, result.ExternalUserId);
        Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task GoogleFitRefreshTokenAsync_WhenTokenResponseIsNull_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("null"));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GoogleFitRefreshTokenAsync_WhenRequestFails_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GoogleFitFetchDailyDataAsync_WithAggregateResponse_MapsKnownDataTypes() {
        var handler = new RecordingHttpMessageHandler(request => {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("access", request.Headers.Authorization.Parameter);
            return JsonResponse("""
                {
                  "bucket": [
                    {
                      "dataset": [
                        { "dataSourceId": "derived:com.google.step_count.delta", "point": [ { "value": [ { "intVal": 1200 } ] } ] },
                        { "dataSourceId": "derived:com.google.calories.expended", "point": [ { "value": [ { "fpVal": 345.5 } ] } ] },
                        { "dataSourceId": "derived:com.google.active_minutes", "point": [ { "value": [ { "intVal": 42 } ] } ] },
                        { "dataSourceId": "derived:com.google.heart_rate.bpm", "point": [ { "value": [ { "fpVal": 61.2 } ] } ] },
                        { "dataSourceId": "derived:unknown", "point": [ { "value": [ { "intVal": 1 } ] } ] }
                      ]
                    }
                  ]
                }
                """);
        });
        GoogleFitClient client = CreateGoogleFitClient(handler);

        IReadOnlyList<WearableDataPoint> result = await client.FetchDailyDataAsync("access", new DateTime(2026, 4, 6), CancellationToken.None);

        Assert.Collection(
            result,
            point => Assert.Equal((WearableDataType.Steps, 1200d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.CaloriesBurned, 345.5d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.ActiveMinutes, 42d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.HeartRate, 61.2d), (point.DataType, point.Value)));
    }

    [Fact]
    public async Task GoogleFitFetchDailyDataAsync_WhenRequestFails_ReturnsEmpty() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        IReadOnlyList<WearableDataPoint> result = await client.FetchDailyDataAsync("access", DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GoogleFitFetchDailyDataAsync_WhenDatasetHasNoPointOrValue_SkipsDataset() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("""
            {
              "bucket": [
                {
                  "dataset": [
                    { "dataSourceId": "derived:com.google.step_count.delta" },
                    { "dataSourceId": "derived:com.google.calories.expended", "point": [ {} ] }
                  ]
                }
              ]
            }
            """));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        IReadOnlyList<WearableDataPoint> result = await client.FetchDailyDataAsync("access", new DateTime(2026, 4, 6), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GoogleFitFetchDailyDataAsync_WhenBucketsOrDatasetsMissing_ReturnsEmpty() {
        var handler = new RecordingHttpMessageHandler(
            _ => JsonResponse("""{}"""),
            _ => JsonResponse("""{ "bucket": [ {} ] }"""));
        GoogleFitClient client = CreateGoogleFitClient(handler);

        IReadOnlyList<WearableDataPoint> noBuckets = await client.FetchDailyDataAsync("access", new DateTime(2026, 4, 6), CancellationToken.None);
        IReadOnlyList<WearableDataPoint> noDatasets = await client.FetchDailyDataAsync("access", new DateTime(2026, 4, 6), CancellationToken.None);

        Assert.Empty(noBuckets);
        Assert.Empty(noDatasets);
    }

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
    public async Task FitbitExchangeCodeAsync_WhenClientIdMissing_ReturnsNullWithoutRequest() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        FitbitClient client = CreateFitbitClient(handler, clientId: "");

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenTokenResponseIsNull_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("null"));
        FitbitClient client = CreateFitbitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitExchangeCodeAsync_WhenTokenRequestFails_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        FitbitClient client = CreateFitbitClient(handler);

        WearableTokenResult? result = await client.ExchangeCodeAsync("code", CancellationToken.None);

        Assert.Null(result);
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
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Equal("fitbit-user", result.ExternalUserId);
        Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc);
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
        Assert.Equal("access-next", result.AccessToken);
        Assert.Equal("refresh-next", result.RefreshToken);
        Assert.Equal("fitbit-user", result.ExternalUserId);
        Assert.Equal(FixedNow.AddSeconds(3600).UtcDateTime, result.ExpiresAtUtc);
    }

    [Fact]
    public async Task FitbitRefreshTokenAsync_WhenTokenResponseIsNull_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse("null"));
        FitbitClient client = CreateFitbitClient(handler);

        WearableTokenResult? result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FitbitRefreshTokenAsync_WhenRequestFails_ReturnsNull() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        FitbitClient client = CreateFitbitClient(handler);

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

        IReadOnlyList<WearableDataPoint> result = await client.FetchDailyDataAsync("access", new DateTime(2026, 4, 6), CancellationToken.None);

        Assert.Collection(
            result,
            point => Assert.Equal((WearableDataType.Steps, 1000d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.CaloriesBurned, 500d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.ActiveMinutes, 25d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.HeartRate, 58d), (point.DataType, point.Value)),
            point => Assert.Equal((WearableDataType.SleepMinutes, 420d), (point.DataType, point.Value)));
    }

    [Fact]
    public async Task FitbitFetchDailyDataAsync_WhenRequestFails_ReturnsEmpty() {
        var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        FitbitClient client = CreateFitbitClient(handler);

        IReadOnlyList<WearableDataPoint> result = await client.FetchDailyDataAsync("access", DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(result);
    }

    private static GoogleFitClient CreateGoogleFitClient(RecordingHttpMessageHandler handler, string clientId = "google-client") {
        return new GoogleFitClient(
            new HttpClient(handler),
            MsOptions.Create(new GoogleFitOptions {
                ClientId = clientId,
                ClientSecret = "google-secret",
                RedirectUri = "https://app.test/google",
            }),
            FixedTime,
            NullLogger<GoogleFitClient>.Instance);
    }

    private static FitbitClient CreateFitbitClient(RecordingHttpMessageHandler handler, string clientId = "fitbit-client") {
        return new FitbitClient(
            new HttpClient(handler),
            MsOptions.Create(new FitbitOptions {
                ClientId = clientId,
                ClientSecret = "fitbit-secret",
                RedirectUri = "https://app.test/fitbit",
            }),
            FixedTime,
            NullLogger<FitbitClient>.Instance);
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

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
}
