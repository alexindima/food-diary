using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using FoodDiary.Telegram.Bot.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotOperationClient(HttpClient client, IOptions<TelegramBotOptions> options) {
    internal const string ClientName = "TelegramOperations";
    private const long MaximumResponseBytes = 131072;

    internal async Task<BotOperationId> RegisterAsync(long updateId, long telegramUserId, string payload, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await CreateSdk().RegisterAsync("1",
            new RegisterTelegramOperationHttpRequest { UpdateId = updateId, TelegramUserId = telegramUserId, Payload = payload },
            RequestContext(), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            ApiErrorHttpResponse error = await ReadAsync<ApiErrorHttpResponse>(response, cancellationToken).ConfigureAwait(false);
            throw new BotOperationApiException(error.Error, response.StatusCode);
        }
        TelegramOperationRegisteredHttpResponse registered = await ReadAsync<TelegramOperationRegisteredHttpResponse>(response, cancellationToken).ConfigureAwait(false);
        if (registered.OperationId == Guid.Empty) {
            throw new InvalidDataException("The API returned an invalid operation ID.");
        }
        return new BotOperationId(registered.OperationId);
    }

    internal async Task<IReadOnlyList<BotOperationId>> ListReadyAsync(CancellationToken cancellationToken) {
        using HttpResponseMessage response = await CreateSdk().ListReadyAsync("1", RequestContext(), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        Guid[] ids = await ReadAsync<Guid[]>(response, cancellationToken).ConfigureAwait(false);
        return ids.Select(id => new BotOperationId(id)).ToArray();
    }

    internal async Task<BotOperationLease?> AcquireAsync(BotOperationId operationId, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await CreateSdk().AcquireAsync(operationId.Value, "1", RequestContext(), cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Conflict) {
            return null;
        }
        response.EnsureSuccessStatusCode();
        BotOperationLease lease = BotApiMapper.Lease(await ReadAsync<TelegramOperationLeaseHttpResponse>(response, cancellationToken).ConfigureAwait(false));
        if (lease.OperationId != operationId || lease.LeaseId == BotLeaseId.Empty || lease.UserId == BotUserId.Empty) {
            throw new InvalidDataException("The API returned an invalid operation lease.");
        }
        return lease;
    }

    internal async Task<bool> CheckpointAsync(BotOperationLease lease, string checkpoint, bool completed,
        DateTime nextAttemptAtUtc, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await CreateSdk().CheckpointAsync(lease.OperationId.Value, "1",
            new CheckpointTelegramOperationHttpRequest { LeaseId = lease.LeaseId.Value, Checkpoint = checkpoint, Completed = completed, NextAttemptAtUtc = nextAttemptAtUtc },
            RequestContext(), cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Conflict) {
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
    }

    private BotOperationsApi CreateSdk() {
        if (!BotUriHelper.TryCreateApiBaseUri(options.Value.ApiBaseUrl, out Uri? baseUri) || string.IsNullOrWhiteSpace(options.Value.ApiSecret)) {
            throw new InvalidOperationException("Telegram operation API is not configured.");
        }
        return new BotOperationsApi(new BotApiTransport(client, baseUri!));
    }

    private BotApiRequestContext RequestContext() => new(ApiSecret: options.Value.ApiSecret);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The API returned an empty operation response.");
    }

}
