using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed class TelegramStarsBillingGateway(HttpClient client, IOptions<TelegramStarsOptions> options,
    TimeProvider timeProvider) : IBillingProviderGateway {
    public string Provider => BillingProviderNames.TelegramStars;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request, CancellationToken cancellationToken = default) {
        if (!options.Value.CanCheckout) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.ProviderNotConfigured(Provider));
        }
        if (request.Plan is not "monthly") {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.InvalidPlan);
        }
        if (request.TelegramUserId is not > 0) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.TelegramNotLinked);
        }
        // An opaque invoice identifies the persisted checkout, never a user-supplied account ID.
        string payload = "stars_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            FormattableString.Invariant($"{request.UserId:N}:{request.TelegramUserId}:{request.IdempotencyKey}")))).ToLowerInvariant();
        int amount = options.Value.PremiumPrice;
        Result<JsonElement> result = await CallAsync("createInvoiceLink", new {
            title = "FoodDiary Premium · 30 days",
            description = "Premium for your FoodDiary account. Renews every 30 days until canceled.",
            payload,
            currency = "XTR",
            provider_token = "",
            subscription_period = 2592000,
            prices = new[] { new { label = "Premium · 30 days", amount } },
        }, cancellationToken);
        if (result.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(result.Error);
        }
        string? url = result.Value.ValueKind == JsonValueKind.String ? result.Value.GetString() : null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? invoice) || invoice.Scheme is not "https" || invoice.Host is not "t.me") {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.ProviderOperationFailed(Provider, "Invalid invoice response."));
        }
        return Result.Success(new BillingCheckoutSessionModel(payload, invoice.AbsoluteUri,
            request.TelegramUserId.Value.ToString(CultureInfo.InvariantCulture) + ":" + request.UserId.ToString("N", CultureInfo.InvariantCulture),
            "stars_30d_" + amount.ToString(CultureInfo.InvariantCulture), "monthly", amount, "XTR"));
    }

    public async Task<Result<BillingCancellationSnapshotModel>> CancelSubscriptionAsync(
        BillingCancellationRequestModel request, CancellationToken cancellationToken = default) {
        string payer = request.ExternalCustomerId.Split(':', 2)[0];
        if (!long.TryParse(payer, CultureInfo.InvariantCulture, out long userId) || userId <= 0 ||
            string.IsNullOrWhiteSpace(request.ExternalPaymentMethodId)) {
            return Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.CancellationUnavailable);
        }
        DateTime end = request.CurrentPeriodEndUtc ?? timeProvider.GetUtcNow().UtcDateTime;
        Result<JsonElement> response = await CallAsync("editUserStarSubscription", new {
            user_id = userId,
            telegram_payment_charge_id = request.ExternalPaymentMethodId,
            is_canceled = true,
        }, cancellationToken);
        if (response.IsSuccess && response.Value.ValueKind is not JsonValueKind.True) {
            return Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.ProviderOperationFailed(Provider, "Cancellation was not confirmed."));
        }
        return response.IsFailure
            ? Result.Failure<BillingCancellationSnapshotModel>(response.Error)
            : Result.Success(new BillingCancellationSnapshotModel("active", request.CurrentPeriodStartUtc, end,
                CancelAtPeriodEnd: true, CanceledAtUtc: null, end, timeProvider.GetUtcNow().UtcDateTime));
    }

    public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<BillingPortalSessionModel>(BillingErrors.CustomerPortalUnavailable));

    // Stars updates arrive only through the authenticated bot API, not a public provider webhook.
    public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(string payload, string signatureHeader,
        CancellationToken cancellationToken = default) => Task.FromResult(
            Result.Failure<BillingWebhookEventModel?>(BillingErrors.WebhookValidationFailed("Use the authenticated Telegram update transport.")));

    private async Task<Result<JsonElement>> CallAsync(string method, object body, CancellationToken cancellationToken) {
        TelegramStarsOptions settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BotToken)) {
            return Result.Failure<JsonElement>(BillingErrors.ProviderNotConfigured(Provider));
        }
        string path = $"https://api.telegram.org/bot{settings.BotToken}/{(settings.UseTestEnvironment ? "test/" : "")}{method}";
        try {
            using HttpResponseMessage response = await client.PostAsJsonAsync(path, body, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                return Result.Failure<JsonElement>(BillingErrors.ProviderOperationFailed(Provider, "Telegram rejected the request."));
            }
            using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;
            return root.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind == JsonValueKind.True &&
                root.TryGetProperty("result", out JsonElement result)
                ? Result.Success(result.Clone())
                : Result.Failure<JsonElement>(BillingErrors.ProviderOperationFailed(Provider, "Invalid Telegram response."));
        } catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested)) {
            // HTTP exception text may contain the token in the request URL.
            return Result.Failure<JsonElement>(BillingErrors.ProviderOperationFailed(Provider, "Telegram is temporarily unavailable."));
        }
    }
}
