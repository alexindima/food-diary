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
    ILogger<GoogleFitClient> logger) : IWearableClient {
    public WearableProvider Provider => WearableProvider.GoogleFit;

    public string GetAuthorizationUrl(string state) {
        var config = options.Value;
        return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={config.ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}" +
               $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/fitness.activity.read https://www.googleapis.com/auth/fitness.heart_rate.read https://www.googleapis.com/auth/fitness.sleep.read")}" +
               $"&state={Uri.EscapeDataString(state)}&access_type=offline&prompt=consent";
    }

    public async Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.ClientId)) {
            return null;
        }

        try {
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["client_id"] = config.ClientId,
                    ["client_secret"] = config.ClientSecret,
                    ["redirect_uri"] = config.RedirectUri,
                }), cancellationToken);

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
            if (token is null) {
                return null;
            }

            // Get user ID from token info
            var userInfo = await httpClient.GetFromJsonAsync<JsonElement>(
                $"https://www.googleapis.com/oauth2/v1/userinfo?access_token={token.AccessToken}", cancellationToken);
            var userId = userInfo.TryGetProperty("id", out var id) ? id.GetString() ?? "unknown" : "unknown";

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken,
                userId,
                DateTime.UtcNow.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit token exchange failed");
            return null;
        }
    }

    public async Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) {
        var config = options.Value;
        try {
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = config.ClientId,
                    ["client_secret"] = config.ClientSecret,
                }), cancellationToken);

            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
            if (token is null) {
                return null;
            }

            return new WearableTokenResult(
                token.AccessToken,
                token.RefreshToken ?? refreshToken,
                string.Empty,
                DateTime.UtcNow.AddSeconds(token.ExpiresIn));
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit token refresh failed");
            return null;
        }
    }

    public async Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(
        string accessToken, DateTime date, CancellationToken cancellationToken = default) {
        var results = new List<WearableDataPoint>();
        var startTimeMillis = new DateTimeOffset(date.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var endTimeMillis = new DateTimeOffset(date.Date.AddDays(1), TimeSpan.Zero).ToUnixTimeMilliseconds();

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

            var response = await httpClient.PostAsJsonAsync(
                "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate",
                aggregateRequest, cancellationToken);

            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            if (data.TryGetProperty("bucket", out var buckets) && buckets.GetArrayLength() > 0) {
                var bucket = buckets[0];
                if (bucket.TryGetProperty("dataset", out var datasets)) {
                    foreach (var dataset in datasets.EnumerateArray()) {
                        if (!dataset.TryGetProperty("point", out var points) || points.GetArrayLength() == 0) {
                            continue;
                        }

                        var dataSourceId = dataset.TryGetProperty("dataSourceId", out var dsId) ? dsId.GetString() : "";
                        var point = points[0];
                        if (!point.TryGetProperty("value", out var values) || values.GetArrayLength() == 0) {
                            continue;
                        }

                        var value = values[0];
                        double numericValue = 0;
                        if (value.TryGetProperty("intVal", out var intVal)) {
                            numericValue = intVal.GetDouble();
                        } else if (value.TryGetProperty("fpVal", out var fpVal)) {
                            numericValue = fpVal.GetDouble();
                        }

                        if (dataSourceId?.Contains("step_count") == true) {
                            results.Add(new WearableDataPoint(WearableDataType.Steps, numericValue));
                        } else if (dataSourceId?.Contains("calories.expended") == true) {
                            results.Add(new WearableDataPoint(WearableDataType.CaloriesBurned, numericValue));
                        } else if (dataSourceId?.Contains("active_minutes") == true) {
                            results.Add(new WearableDataPoint(WearableDataType.ActiveMinutes, numericValue));
                        } else if (dataSourceId?.Contains("heart_rate") == true) {
                            results.Add(new WearableDataPoint(WearableDataType.HeartRate, numericValue));
                        }
                    }
                }
            }
        } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) {
            logger.LogWarning(ex, "Google Fit data fetch failed for {Date}", date.ToString("yyyy-MM-dd"));
        }

        return results;
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
