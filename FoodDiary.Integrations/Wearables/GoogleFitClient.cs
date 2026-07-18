using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Wearables;

internal sealed class GoogleFitClient(
    HttpClient httpClient,
    IOptions<GoogleFitOptions> options,
    TimeProvider timeProvider,
    ILogger<GoogleFitClient> logger) : IWearableClient {
    public WearableProvider Provider => WearableProvider.GoogleFit;

    public string GetAuthorizationUrl(string state) {
        GoogleFitOptions config = options.Value;
        return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={config.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}" +
               $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/fitness.activity.read https://www.googleapis.com/auth/fitness.heart_rate.read https://www.googleapis.com/auth/fitness.sleep.read")}" +
               $"&state={Uri.EscapeDataString(state)}&access_type=offline&prompt=consent";
    }

    public async Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        GoogleFitOptions config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ClientId)) {
            return null;
        }

        try {
            HttpResponseMessage response = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["client_id"] = config.ClientId,
                    ["client_secret"] = config.ClientSecret,
                    ["redirect_uri"] = config.RedirectUri,
                }), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            GoogleTokenResponse? token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (token is null) {
                return null;
            }

            using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v1/userinfo");
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            using HttpResponseMessage userInfoResponse = await httpClient.SendAsync(userInfoRequest, cancellationToken).ConfigureAwait(false);
            userInfoResponse.EnsureSuccessStatusCode();
            JsonElement userInfo = await userInfoResponse.Content
                .ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            string userId = userInfo.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "unknown" : "unknown";

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                userId,
                timeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit token exchange failed");
            return null;
        }
    }

    public async Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
        GoogleFitOptions config = options.Value;
        try {
            HttpResponseMessage response = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = config.ClientId,
                    ["client_secret"] = config.ClientSecret,
                }), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            GoogleTokenResponse? token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken ?? refreshToken,
                string.Empty,
                timeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit token refresh failed");
            return null;
        }
    }

    public async Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(
        string accessToken, DateTime date, CancellationToken cancellationToken = default) {
        var results = new List<WearableDataPoint>();
        long startTimeMillis = new DateTimeOffset(date.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        long endTimeMillis = new DateTimeOffset(date.Date.AddDays(1), TimeSpan.Zero).ToUnixTimeMilliseconds();

        try {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var aggregateRequest = new {
                aggregateBy = new[] {
                    new { dataTypeName = "com.google.step_count.delta" },
                    new { dataTypeName = "com.google.calories.expended" },
                    new { dataTypeName = "com.google.active_minutes" },
                    new { dataTypeName = "com.google.heart_rate.bpm" },
                },
                bucketByTime = new { durationMillis = 86400000 },
                startTimeMillis,
                endTimeMillis,
            };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate",
                aggregateRequest, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);
            results.AddRange(ParseDailyData(data));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit data fetch failed for {Date}", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        return results;
    }

    private static IReadOnlyList<WearableDataPoint> ParseDailyData(JsonElement data) {
        if (!data.TryGetProperty("bucket", out JsonElement buckets) || buckets.GetArrayLength() == 0) {
            return [];
        }

        JsonElement bucket = buckets[0];
        if (!bucket.TryGetProperty("dataset", out JsonElement datasets)) {
            return [];
        }

        var results = new List<WearableDataPoint>();
        foreach (JsonElement dataset in datasets.EnumerateArray()) {
            WearableDataPoint? dataPoint = TryCreateDataPoint(dataset);
            if (dataPoint is not null) {
                results.Add(dataPoint);
            }
        }

        return results;
    }

    private static WearableDataPoint? TryCreateDataPoint(JsonElement dataset) {
        if (!dataset.TryGetProperty("point", out JsonElement points) || points.GetArrayLength() == 0) {
            return null;
        }

        string? dataSourceId = dataset.TryGetProperty("dataSourceId", out JsonElement dsId) ? dsId.GetString() : string.Empty;
        JsonElement point = points[0];
        if (!point.TryGetProperty("value", out JsonElement values) || values.GetArrayLength() == 0) {
            return null;
        }

        double numericValue = ReadNumericValue(values[0]);
        if (dataSourceId?.Contains("step_count", StringComparison.Ordinal) == true) {
            return new WearableDataPoint(WearableDataType.Steps, numericValue);
        }

        if (dataSourceId?.Contains("calories.expended", StringComparison.Ordinal) == true) {
            return new WearableDataPoint(WearableDataType.CaloriesBurned, numericValue);
        }

        if (dataSourceId?.Contains("active_minutes", StringComparison.Ordinal) == true) {
            return new WearableDataPoint(WearableDataType.ActiveMinutes, numericValue);
        }

        return dataSourceId?.Contains("heart_rate", StringComparison.Ordinal) == true
            ? new WearableDataPoint(WearableDataType.HeartRate, numericValue)
            : null;
    }

    private static double ReadNumericValue(JsonElement value) {
        if (value.TryGetProperty("intVal", out JsonElement intVal)) {
            return intVal.GetDouble();
        }

        return value.TryGetProperty("fpVal", out JsonElement fpVal) ? fpVal.GetDouble() : 0;
    }

    private sealed class GoogleTokenResponse {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
