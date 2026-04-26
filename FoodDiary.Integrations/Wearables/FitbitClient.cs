using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Wearables;

internal sealed class FitbitClient(
    HttpClient httpClient,
    IOptions<FitbitOptions> options,
    ILogger<FitbitClient> logger) : IWearableClient {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public WearableProvider Provider => WearableProvider.Fitbit;

    public string GetAuthorizationUrl(string state) {
        var config = options.Value;
        return $"https://www.fitbit.com/oauth2/authorize?response_type=code&client_id={config.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}&scope=activity+heartrate+sleep" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ClientId)) {
            return null;
        }

        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fitbit.com/oauth2/token") {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = config.RedirectUri,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<FitbitTokenResponse>(JsonOptions, cancellationToken);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                token.UserId,
                DateTime.UtcNow.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Fitbit token exchange failed");
            return null;
        }
    }

    public async Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
        var config = options.Value;
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fitbit.com/oauth2/token") {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<FitbitTokenResponse>(JsonOptions, cancellationToken);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                token.UserId,
                DateTime.UtcNow.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Fitbit token refresh failed");
            return null;
        }
    }

    public async Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(
        string accessToken, DateTime date, CancellationToken cancellationToken = default) {
        var dateStr = date.ToString("yyyy-MM-dd");
        var results = new List<WearableDataPoint>();

        try {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Fetch activity summary (steps, calories, active minutes)
            var activityUrl = $"https://api.fitbit.com/1/user/-/activities/date/{dateStr}.json";
            var activityResponse = await httpClient.GetFromJsonAsync<JsonElement>(activityUrl, cancellationToken);

            if (activityResponse.TryGetProperty("summary", out var summary)) {
                if (summary.TryGetProperty("steps", out var steps)) {
                    results.Add(new WearableDataPoint(WearableDataType.Steps, steps.GetDouble()));
                }
                if (summary.TryGetProperty("caloriesOut", out var cals)) {
                    results.Add(new WearableDataPoint(WearableDataType.CaloriesBurned, cals.GetDouble()));
                }
                if (summary.TryGetProperty("veryActiveMinutes", out var veryActive) &&
                    summary.TryGetProperty("fairlyActiveMinutes", out var fairlyActive)) {
                    results.Add(new WearableDataPoint(WearableDataType.ActiveMinutes,
                        veryActive.GetDouble() + fairlyActive.GetDouble()));
                }
            }

            // Fetch resting heart rate
            var heartUrl = $"https://api.fitbit.com/1/user/-/activities/heart/date/{dateStr}/1d.json";
            var heartResponse = await httpClient.GetFromJsonAsync<JsonElement>(heartUrl, cancellationToken);

            if (heartResponse.TryGetProperty("activities-heart", out var heartArray) &&
                heartArray.GetArrayLength() > 0) {
                var heartDay = heartArray[0];
                if (heartDay.TryGetProperty("value", out var heartValue) &&
                    heartValue.TryGetProperty("restingHeartRate", out var rhr)) {
                    results.Add(new WearableDataPoint(WearableDataType.HeartRate, rhr.GetDouble()));
                }
            }

            // Fetch sleep
            var sleepUrl = $"https://api.fitbit.com/1.2/user/-/sleep/date/{dateStr}.json";
            var sleepResponse = await httpClient.GetFromJsonAsync<JsonElement>(sleepUrl, cancellationToken);

            if (sleepResponse.TryGetProperty("summary", out var sleepSummary) &&
                sleepSummary.TryGetProperty("totalMinutesAsleep", out var sleepMinutes)) {
                results.Add(new WearableDataPoint(WearableDataType.SleepMinutes, sleepMinutes.GetDouble()));
            }
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Fitbit data fetch failed for {Date}", dateStr);
        }

        return results;
    }

    private sealed class FitbitTokenResponse {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; init; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
