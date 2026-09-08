using System.Net.Http.Json;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Client.Journal;

public sealed class MailRelayJournalClient(HttpClient httpClient, IOptions<MailRelayClientOptions> options) : IMailRelayJournalClient {
    public async Task<OutgoingEmailJournalPage> GetPageAsync(int page, int limit, string? purpose, string? status, string? recipient, CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? id = null, string? correlationId = null) {
        string path = $"/api/email/messages?page={page.ToString(System.Globalization.CultureInfo.InvariantCulture)}&limit={limit.ToString(System.Globalization.CultureInfo.InvariantCulture)}&purpose={Uri.EscapeDataString(purpose ?? "")}&status={Uri.EscapeDataString(status ?? "")}&recipient={Uri.EscapeDataString(recipient ?? "")}";
        path += $"&fromUtc={Uri.EscapeDataString(fromUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "")}&toUtc={Uri.EscapeDataString(toUtc?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "")}&id={id}&correlationId={Uri.EscapeDataString(correlationId ?? "")}";
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Relay-Api-Key", options.Value.ApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OutgoingEmailJournalPage>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Mail relay returned an empty journal response.");
    }
}
