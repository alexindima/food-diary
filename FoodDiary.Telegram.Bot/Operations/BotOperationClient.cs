using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotOperationClient(HttpClient client, IOptions<TelegramBotOptions> options) {
    internal const string ClientName = "TelegramOperations";
    private const string Root = "/api/v1/auth/telegram/bot/operations";
    private const long MaximumResponseBytes = 131072;

    internal async Task<Guid> RegisterAsync(long updateId, long telegramUserId, string payload, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, Root);
        request.Content = JsonContent.Create(new { UpdateId = updateId, TelegramUserId = telegramUserId, Payload = payload });
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            ApiError error = await ReadAsync<ApiError>(response, cancellationToken).ConfigureAwait(false);
            throw new BotOperationApiException(error.Error, response.StatusCode);
        }
        Registered registered = await ReadAsync<Registered>(response, cancellationToken).ConfigureAwait(false);
        if (registered.OperationId == Guid.Empty) {
            throw new InvalidDataException("The API returned an invalid operation ID.");
        }
        return registered.OperationId;
    }

    internal async Task<IReadOnlyList<Guid>> ListReadyAsync(CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, Root + "/ready");
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<Guid[]>(response, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<BotOperationLease?> AcquireAsync(Guid operationId, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"{Root}/{operationId:D}/lease");
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Conflict) {
            return null;
        }
        response.EnsureSuccessStatusCode();
        BotOperationLease lease = await ReadAsync<BotOperationLease>(response, cancellationToken).ConfigureAwait(false);
        if (lease.OperationId != operationId || lease.LeaseId == Guid.Empty || lease.UserId == Guid.Empty) {
            throw new InvalidDataException("The API returned an invalid operation lease.");
        }
        return lease;
    }

    internal async Task<bool> CheckpointAsync(BotOperationLease lease, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"{Root}/{lease.OperationId:D}/checkpoint");
        request.Content = JsonContent.Create(new { lease.LeaseId, Checkpoint = checkpoint, Completed = completed, NextAttemptAtUtc = nextAttemptAtUtc });
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Conflict) {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path) {
        if (!BotUriHelper.TryCreateApiBaseUri(options.Value.ApiBaseUrl, out Uri? baseUri) || string.IsNullOrWhiteSpace(options.Value.ApiSecret)) {
            throw new InvalidOperationException("Telegram operation API is not configured.");
        }
        var request = new HttpRequestMessage(method, new Uri(baseUri!, path));
        request.Headers.Add("X-Telegram-Bot-Secret", options.Value.ApiSecret);
        return request;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The API returned an empty operation response.");
    }

    private sealed record Registered(Guid OperationId);
    private sealed record ApiError(string Error);
}
