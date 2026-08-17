using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class PaddleNotificationRecoveryService(
    HttpClient httpClient,
    IOptions<PaddleOptions> options,
    TimeProvider? timeProvider = null) {
    private const int MaximumPages = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };
    private readonly PaddleOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<PaddleNotificationRecoveryResult> ReplayFailedAsync(
        CancellationToken cancellationToken = default) {
        if (!PaddleOptions.HasValidConfiguration(_options) ||
            !PaddleOptions.HasConfiguredNotificationRecovery(_options)) {
            return new PaddleNotificationRecoveryResult(0, 0);
        }

        ConfigureClient();
        DateTimeOffset from = _timeProvider.GetUtcNow().AddDays(-90);
        string? next = $"notifications?status=failed&notification_setting_id={Uri.EscapeDataString(_options.NotificationSettingId.Trim())}" +
            $"&from={Uri.EscapeDataString(from.ToString("O", CultureInfo.InvariantCulture))}&per_page=200&order_by=id[ASC]";
        int inspected = 0;
        int replayed = 0;

        for (int page = 0; page < MaximumPages && !string.IsNullOrWhiteSpace(next); page++) {
            using var listRequest = new HttpRequestMessage(HttpMethod.Get, next);
            using HttpResponseMessage response = await httpClient.SendAsync(
                listRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            ListNotificationsResponse payload = await BoundedHttpContentReader.ReadFromJsonAsync<ListNotificationsResponse>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false) ?? throw new JsonException("Paddle notifications response was empty.");

            foreach (NotificationResponse notification in payload.Data) {
                inspected++;
                if (!string.Equals(notification.Origin, "event", StringComparison.Ordinal) ||
                    notification.ReplayedAt is not null) {
                    continue;
                }

                using var replayRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"notifications/{Uri.EscapeDataString(notification.Id)}/replay");
                using HttpResponseMessage replayResponse = await httpClient.SendAsync(
                    replayRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                replayResponse.EnsureSuccessStatusCode();
                replayed++;
            }

            next = NormalizeNext(payload.Meta?.Pagination?.Next);
        }

        return new PaddleNotificationRecoveryResult(inspected, replayed);
    }

    private void ConfigureClient() {
        httpClient.BaseAddress ??= new Uri($"{_options.ApiBaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey.Trim());
        if (!httpClient.DefaultRequestHeaders.Contains("Paddle-Version")) {
            httpClient.DefaultRequestHeaders.Add("Paddle-Version", "1");
        }
    }

    private static string? NormalizeNext(string? next) {
        if (string.IsNullOrWhiteSpace(next)) {
            return null;
        }

        return Uri.TryCreate(next, UriKind.Absolute, out Uri? absolute)
            ? $"{absolute.PathAndQuery.TrimStart('/')}"
            : next.TrimStart('/');
    }

    private sealed record ListNotificationsResponse(
        IReadOnlyList<NotificationResponse> Data,
        ResponseMeta? Meta);

    private sealed record NotificationResponse(
        string Id,
        string Origin,
        [property: JsonPropertyName("replayed_at")] DateTimeOffset? ReplayedAt);

    private sealed record ResponseMeta(PaginationMeta? Pagination);

    private sealed record PaginationMeta(string? Next);
}
