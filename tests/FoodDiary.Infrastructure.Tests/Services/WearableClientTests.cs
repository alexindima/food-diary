using System.Net;
using System.Text;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Wearables;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class WearableClientTests {
    [Fact]
    public void FitbitGetAuthorizationUrl_EncodesRedirectUriStateAndScopes() {
        var client = new FitbitClient(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new FitbitOptions {
                ClientId = "fitbit-client",
                RedirectUri = "https://app.example/auth/callback?provider=fitbit",
            }),
            NullLogger<FitbitClient>.Instance);

        var url = client.GetAuthorizationUrl("state value/1");

        Assert.Contains("client_id=fitbit-client", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Fapp.example%2Fauth%2Fcallback%3Fprovider%3Dfitbit", url);
        Assert.Contains("scope=activity+heartrate+sleep", url);
        Assert.Contains("state=state%20value%2F1", url);
    }

    [Fact]
    public async Task FitbitExchangeCode_SendsBasicAuthAndMapsTokenResponse() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "access_token": "access",
                  "refresh_token": "refresh",
                  "user_id": "fitbit-user",
                  "expires_in": 3600
                }
                """),
        });
        var client = new FitbitClient(
            new HttpClient(handler),
            MsOptions.Create(new FitbitOptions {
                ClientId = "client",
                ClientSecret = "secret",
                RedirectUri = "https://app.example/fitbit",
            }),
            NullLogger<FitbitClient>.Instance);

        var result = await client.ExchangeCodeAsync("auth-code", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Equal("fitbit-user", result.ExternalUserId);
        Assert.Equal("Basic", handler.Requests.Single().Headers.Authorization?.Scheme);
        Assert.Contains("grant_type=authorization_code", handler.RequestBodies.Single());
        Assert.Contains("code=auth-code", handler.RequestBodies.Single());
    }

    [Fact]
    public async Task FitbitFetchDailyData_MapsActivityHeartRateAndSleepResponses() {
        var handler = new QueueHttpMessageHandler([
            JsonResponse("""
                {
                  "summary": {
                    "steps": 8100,
                    "caloriesOut": 450,
                    "veryActiveMinutes": 20,
                    "fairlyActiveMinutes": 15
                  }
                }
                """),
            JsonResponse("""
                {
                  "activities-heart": [
                    { "value": { "restingHeartRate": 62 } }
                  ]
                }
                """),
            JsonResponse("""
                {
                  "summary": { "totalMinutesAsleep": 430 }
                }
                """),
        ]);
        var client = new FitbitClient(
            new HttpClient(handler),
            MsOptions.Create(new FitbitOptions { ClientId = "client", ClientSecret = "secret" }),
            NullLogger<FitbitClient>.Instance);

        var result = await client.FetchDailyDataAsync(
            "access-token",
            new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Equal("Bearer", handler.Requests[0].Headers.Authorization?.Scheme);
        Assert.Contains(result, p => p.DataType == WearableDataType.Steps && p.Value == 8100);
        Assert.Contains(result, p => p.DataType == WearableDataType.CaloriesBurned && p.Value == 450);
        Assert.Contains(result, p => p.DataType == WearableDataType.ActiveMinutes && p.Value == 35);
        Assert.Contains(result, p => p.DataType == WearableDataType.HeartRate && p.Value == 62);
        Assert.Contains(result, p => p.DataType == WearableDataType.SleepMinutes && p.Value == 430);
    }

    [Fact]
    public void GoogleFitGetAuthorizationUrl_EncodesScopeStateAndOfflinePrompt() {
        var client = new GoogleFitClient(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new GoogleFitOptions {
                ClientId = "google-client",
                RedirectUri = "https://app.example/auth/google-fit",
            }),
            NullLogger<GoogleFitClient>.Instance);

        var url = client.GetAuthorizationUrl("state value/1");

        Assert.Contains("client_id=google-client", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Fapp.example%2Fauth%2Fgoogle-fit", url);
        Assert.Contains("fitness.activity.read", url);
        Assert.Contains("state=state%20value%2F1", url);
        Assert.Contains("access_type=offline", url);
        Assert.Contains("prompt=consent", url);
    }

    [Fact]
    public async Task GoogleFitExchangeCode_MapsTokenAndFetchesExternalUserId() {
        var handler = new QueueHttpMessageHandler([
            JsonResponse("""
                {
                  "access_token": "access",
                  "refresh_token": "refresh",
                  "expires_in": 3600
                }
                """),
            JsonResponse("""{ "id": "google-user" }"""),
        ]);
        var client = new GoogleFitClient(
            new HttpClient(handler),
            MsOptions.Create(new GoogleFitOptions {
                ClientId = "client",
                ClientSecret = "secret",
                RedirectUri = "https://app.example/google-fit",
            }),
            NullLogger<GoogleFitClient>.Instance);

        var result = await client.ExchangeCodeAsync("auth-code", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        Assert.Equal("google-user", result.ExternalUserId);
        Assert.Contains("grant_type=authorization_code", handler.RequestBodies[0]);
        Assert.Contains("code=auth-code", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task GoogleFitFetchDailyData_MapsAggregateBucketDataPoints() {
        var handler = new QueueHttpMessageHandler([
            JsonResponse("""
                {
                  "bucket": [
                    {
                      "dataset": [
                        {
                          "dataSourceId": "derived:com.google.step_count.delta:com.google.android.gms",
                          "point": [ { "value": [ { "intVal": 7000 } ] } ]
                        },
                        {
                          "dataSourceId": "derived:com.google.calories.expended:com.google.android.gms",
                          "point": [ { "value": [ { "fpVal": 320.5 } ] } ]
                        },
                        {
                          "dataSourceId": "derived:com.google.active_minutes:com.google.android.gms",
                          "point": [ { "value": [ { "intVal": 42 } ] } ]
                        },
                        {
                          "dataSourceId": "derived:com.google.heart_rate.bpm:com.google.android.gms",
                          "point": [ { "value": [ { "fpVal": 61.5 } ] } ]
                        }
                      ]
                    }
                  ]
                }
                """),
        ]);
        var client = new GoogleFitClient(
            new HttpClient(handler),
            MsOptions.Create(new GoogleFitOptions { ClientId = "client", ClientSecret = "secret" }),
            NullLogger<GoogleFitClient>.Instance);

        var result = await client.FetchDailyDataAsync(
            "access-token",
            new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Equal("Bearer", handler.Requests.Single().Headers.Authorization?.Scheme);
        Assert.Contains(result, p => p.DataType == WearableDataType.Steps && p.Value == 7000);
        Assert.Contains(result, p => p.DataType == WearableDataType.CaloriesBurned && p.Value == 320.5);
        Assert.Contains(result, p => p.DataType == WearableDataType.ActiveMinutes && p.Value == 42);
        Assert.Contains(result, p => p.DataType == WearableDataType.HeartRate && p.Value == 61.5);
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = JsonContent(json) };

    private sealed class RecordingHttpMessageHandler(HttpResponseMessage? response = null) : HttpMessageHandler {
        private readonly HttpResponseMessage _response = response ?? JsonResponse("{}");
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return _response;
        }
    }

    private sealed class QueueHttpMessageHandler(IReadOnlyList<HttpResponseMessage> responses) : HttpMessageHandler {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responses.Dequeue();
        }
    }
}
