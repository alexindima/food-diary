using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Wearables;

internal sealed class FitbitClient(
    HttpClient httpClient,
    IOptions<FitbitOptions> options,
    TimeProvider timeProvider,
    ILogger<FitbitClient> logger) : IWearableClient {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public WearableProvider Provider => WearableProvider.Fitbit;

    public string GetAuthorizationUrl(string state) {
        FitbitOptions config = options.Value;
        return $"https://www.fitbit.com/oauth2/authorize?response_type=code&client_id={config.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}&scope=activity+heartrate+sleep" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        FitbitOptions config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ClientId)) {
            return null;
        }

        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fitbit.com/oauth2/token") {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = config.RedirectUri,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            FitbitTokenResponse? token = await response.Content.ReadFromJsonAsync<FitbitTokenResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                token.UserId,
                timeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Fitbit token exchange failed");
            return null;
        }
    }

    public async Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
        FitbitOptions config = options.Value;
        try {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fitbit.com/oauth2/token") {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}")));

            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            FitbitTokenResponse? token = await response.Content.ReadFromJsonAsync<FitbitTokenResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                token.UserId,
                timeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Fitbit token refresh failed");
            return null;
        }
    }

    public async Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(
        string accessToken, DateTime date, CancellationToken cancellationToken = default) {
        string dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var results = new List<WearableDataPoint>();

        try {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Fetch activity summary (steps, calories, active minutes)
            string activityUrl = $"https://api.fitbit.com/1/user/-/activities/date/{dateStr}.json";
            JsonElement activityResponse = await httpClient.GetFromJsonAsync<JsonElement>(activityUrl, cancellationToken).ConfigureAwait(false);

            if (activityResponse.TryGetProperty("summary", out JsonElement summary)) {
                if (summary.TryGetProperty("steps", out JsonElement steps)) {
                    results.Add(new WearableDataPoint(WearableDataType.Steps, steps.GetDouble()));
                }
                if (summary.TryGetProperty("caloriesOut", out JsonElement cals)) {
                    results.Add(new WearableDataPoint(WearableDataType.CaloriesBurned, cals.GetDouble()));
                }
                if (summary.TryGetProperty("veryActiveMinutes", out JsonElement veryActive) &&
                    summary.TryGetProperty("fairlyActiveMinutes", out JsonElement fairlyActive)) {
                    results.Add(new WearableDataPoint(WearableDataType.ActiveMinutes,
                        veryActive.GetDouble() + fairlyActive.GetDouble()));
                }
            }

            // Fetch resting heart rate
            string heartUrl = $"https://api.fitbit.com/1/user/-/activities/heart/date/{dateStr}/1d.json";
            JsonElement heartResponse = await httpClient.GetFromJsonAsync<JsonElement>(heartUrl, cancellationToken).ConfigureAwait(false);

            if (heartResponse.TryGetProperty("activities-heart", out JsonElement heartArray) &&
                heartArray.GetArrayLength() > 0) {
                JsonElement heartDay = heartArray[0];
                if (heartDay.TryGetProperty("value", out JsonElement heartValue) &&
                    heartValue.TryGetProperty("restingHeartRate", out JsonElement rhr)) {
                    results.Add(new WearableDataPoint(WearableDataType.HeartRate, rhr.GetDouble()));
                }
            }

            // Fetch sleep
            string sleepUrl = $"https://api.fitbit.com/1.2/user/-/sleep/date/{dateStr}.json";
            JsonElement sleepResponse = await httpClient.GetFromJsonAsync<JsonElement>(sleepUrl, cancellationToken).ConfigureAwait(false);

            if (sleepResponse.TryGetProperty("summary", out JsonElement sleepSummary) &&
                sleepSummary.TryGetProperty("totalMinutesAsleep", out JsonElement sleepMinutes)) {
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
