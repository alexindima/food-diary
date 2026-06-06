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

    public async Task<IReadOnlyList<InboundMailMessageSummaryResponse>> GetMessagesAsync(
        int? limit,
        CancellationToken cancellationToken) {
        EnsureBaseAddress();

        string path = limit.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"/api/mail-inbox/messages?limit={limit.Value}"
)
            : "/api/mail-inbox/messages";
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, path);
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

        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"/api/mail-inbox/messages/{id}");
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

    private void EnsureBaseAddress() {
        if (httpClient.BaseAddress is null) {
            throw new InvalidOperationException("MailInbox client base URL is not configured.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path) {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey)) {
            request.Headers.TryAddWithoutValidation("X-MailInbox-Api-Key", _options.ApiKey);
        }

        return request;
    }
}
