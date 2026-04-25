using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Client;

public sealed class MailRelayClient(HttpClient httpClient, IOptions<MailRelayClientOptions> options) : IMailRelayClient {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MailRelayClientOptions _options = options.Value;

    public async Task<EnqueueMailRelayEmailResponse> EnqueueAsync(
        EnqueueMailRelayEmailRequest request,
        CancellationToken cancellationToken) {
        if (httpClient.BaseAddress is null) {
            throw new InvalidOperationException("Mail relay client base URL is not configured.");
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/email/send") {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey)) {
            requestMessage.Headers.TryAddWithoutValidation("X-Relay-Api-Key", _options.ApiKey);
        }

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0) {
            throw new InvalidOperationException("Mail relay returned an empty enqueue response.");
        }

        EnqueueMailRelayEmailResponse? payload;
        try {
            payload = await response.Content.ReadFromJsonAsync<EnqueueMailRelayEmailResponse>(
                JsonOptions,
                cancellationToken);
        } catch (JsonException ex) {
            throw new InvalidOperationException("Mail relay returned an invalid enqueue response.", ex);
        }

        return payload ?? throw new InvalidOperationException("Mail relay returned an empty enqueue response.");
    }
}
