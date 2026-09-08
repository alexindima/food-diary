using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Client;

public sealed class MailInboxClient(HttpClient httpClient, IOptions<MailInboxClientOptions> options) : IMailInboxClient {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MailInboxClientOptions _options = options.Value;

    public async Task<InboundMailMessagePageResponse> GetMessagePageAsync(
        int page, int limit, string? recipient, string? category, bool? unread, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? search = null, string? fromAddress = null, Guid? id = null) {
        EnsureBaseAddress();
        string path = string.Create(CultureInfo.InvariantCulture, $"/api/mail-inbox/messages/page?page={page}&limit={limit}");
        if (!string.IsNullOrWhiteSpace(recipient)) { path += $"&recipient={Uri.EscapeDataString(recipient.Trim())}"; }
        if (!string.IsNullOrWhiteSpace(category)) { path += $"&category={Uri.EscapeDataString(category)}"; }
        if (unread.HasValue) { path += $"&unread={unread.Value.ToString().ToLowerInvariant()}"; }
        if (fromUtc.HasValue) { path += "&fromUtc=" + Uri.EscapeDataString(fromUtc.Value.ToString("O", CultureInfo.InvariantCulture)); }
        if (toUtc.HasValue) { path += "&toUtc=" + Uri.EscapeDataString(toUtc.Value.ToString("O", CultureInfo.InvariantCulture)); }
        if (!string.IsNullOrWhiteSpace(search)) { path += "&search=" + Uri.EscapeDataString(search.Trim()); }
        if (!string.IsNullOrWhiteSpace(fromAddress)) { path += "&fromAddress=" + Uri.EscapeDataString(fromAddress.Trim()); }
        if (id.HasValue) { path += "&id=" + id.Value.ToString(); }
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, path, _options.MetadataApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InboundMailMessagePageResponse>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("MailInbox returned an empty message page response.");
    }

    public Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(int? limit, CancellationToken cancellationToken) =>
        GetFilteredMessagesAsync(limit, recipient: null, category: null, unread: null, cancellationToken);

    public async Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetFilteredMessagesAsync(
        int? limit, string? recipient, string? category, bool? unread, CancellationToken cancellationToken) {
        EnsureBaseAddress();

        string path = limit.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"/api/mail-inbox/messages?limit={limit.Value}"
)
            : "/api/mail-inbox/messages";
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(recipient)) { filters.Add($"recipient={Uri.EscapeDataString(recipient.Trim())}"); }
        if (!string.IsNullOrWhiteSpace(category)) { filters.Add($"category={Uri.EscapeDataString(category)}"); }
        if (unread.HasValue) { filters.Add($"unread={unread.Value.ToString().ToLowerInvariant()}"); }
        if (filters.Count > 0) { path += (path.Contains('?', StringComparison.Ordinal) ? "&" : "?") + string.Join('&', filters); }
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, path, _options.MetadataApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<InboundMailMessageSummaryResponse>? payload;
        try {
            payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<InboundMailMessageSummaryResponse>>(
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        } catch (JsonException ex) {
            throw new InvalidOperationException("MailInbox returned an invalid message list response.", ex);
        }

        return payload ?? throw new InvalidOperationException("MailInbox returned an empty message list response.");
    }

    public async Task<InboundMailMessageDetailsResponse?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken) {
        EnsureBaseAddress();

        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Get,
            $"/api/mail-inbox/messages/{id}",
            _options.ContentApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }

        response.EnsureSuccessStatusCode();

        InboundMailMessageDetailsResponse? payload;
        try {
            payload = await response.Content.ReadFromJsonAsync<InboundMailMessageDetailsResponse>(
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        } catch (JsonException ex) {
            throw new InvalidOperationException("MailInbox returned an invalid message details response.", ex);
        }

        return payload ?? throw new InvalidOperationException("MailInbox returned an empty message details response.");
    }

    public async Task<bool> MarkMessageReadAsync(
        Guid id,
        CancellationToken cancellationToken) {
        EnsureBaseAddress();

        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"/api/mail-inbox/messages/{id}/read",
            _options.StateApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private void EnsureBaseAddress() {
        if (httpClient.BaseAddress is null) {
            throw new InvalidOperationException("MailInbox client base URL is not configured.");
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string apiKey) {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(apiKey)) {
            request.Headers.TryAddWithoutValidation("X-MailInbox-Api-Key", apiKey);
        }

        return request;
    }
}
