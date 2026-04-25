using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.MailInbox.Client;

public sealed class MailInboxClient(HttpClient httpClient) : IMailInboxClient {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(
        int? limit,
        CancellationToken cancellationToken) {
        EnsureBaseAddress();

        var path = limit.HasValue
            ? $"/api/mail-inbox/messages?limit={limit.Value}"
            : "/api/mail-inbox/messages";
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<InboundMailMessageSummaryResponse>? payload;
        try {
            payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<InboundMailMessageSummaryResponse>>(
                JsonOptions,
                cancellationToken);
        } catch (JsonException ex) {
            throw new InvalidOperationException("MailInbox returned an invalid message list response.", ex);
        }

        return payload ?? throw new InvalidOperationException("MailInbox returned an empty message list response.");
    }

    public async Task<InboundMailMessageDetailsResponse?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken) {
        EnsureBaseAddress();

        using var response = await httpClient.GetAsync($"/api/mail-inbox/messages/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }

        response.EnsureSuccessStatusCode();

        InboundMailMessageDetailsResponse? payload;
        try {
            payload = await response.Content.ReadFromJsonAsync<InboundMailMessageDetailsResponse>(
                JsonOptions,
                cancellationToken);
        } catch (JsonException ex) {
            throw new InvalidOperationException("MailInbox returned an invalid message details response.", ex);
        }

        return payload ?? throw new InvalidOperationException("MailInbox returned an empty message details response.");
    }

    private void EnsureBaseAddress() {
        if (httpClient.BaseAddress is null) {
            throw new InvalidOperationException("MailInbox client base URL is not configured.");
        }
    }
}
