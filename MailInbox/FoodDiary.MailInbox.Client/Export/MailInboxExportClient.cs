using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailInbox.Client.Models;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Client.Export;

public sealed class MailInboxExportClient(HttpClient httpClient, IOptions<MailInboxClientOptions> options) : IMailInboxExportClient {
    public async Task<IReadOnlyList<MailInboxExportEntryResponse>> GetPageAsync(string recipient,
        DateTimeOffset? beforeReceivedAtUtc, Guid? beforeId, CancellationToken cancellationToken) {
        string path = "/api/mail-inbox/export?limit=50&recipient=" + Uri.EscapeDataString(recipient);
        if (beforeReceivedAtUtc.HasValue && beforeId.HasValue) {
            path += "&beforeReceivedAtUtc=" + Uri.EscapeDataString(beforeReceivedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture)) + "&beforeId=" + beforeId.Value;
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-MailInbox-Api-Key", options.Value.MetadataApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MailInboxExportEntryResponse[]>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("MailInbox returned an empty export response.");
    }

    public async Task<byte[]?> GetMimeAsync(Guid id, string recipient, CancellationToken cancellationToken) {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/mail-inbox/export/{id}/mime?recipient=" + Uri.EscapeDataString(recipient));
        request.Headers.Add("X-MailInbox-Api-Key", options.Value.ContentApiKey);
        using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return null;
        }
        response.EnsureSuccessStatusCode();
        const int maxBytes = 10 * 1024 * 1024;
        if (response.Content.Headers.ContentLength > maxBytes) {
            throw new InvalidOperationException("MailInbox MIME exceeds the export size limit.");
        }
        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable streamScope = stream.ConfigureAwait(false);
        var output = new MemoryStream();
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable outputScope = output.ConfigureAwait(false);
        byte[] buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
            if (output.Length + read > maxBytes) {
                throw new InvalidOperationException("MailInbox MIME exceeds the export size limit.");
            }
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return output.ToArray();
    }
}
