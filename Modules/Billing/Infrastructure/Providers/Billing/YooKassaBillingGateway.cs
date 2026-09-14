using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed class YooKassaBillingGateway(
    HttpClient httpClient,
    IOptions<YooKassaOptions> options)
    : IBillingProviderGateway, IBillingRecurringProviderGateway {
    private const int MaximumPaymentIdLength = 128;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        MaxDepth = FoodDiary.Integrations.Http.BoundedHttpContentReader.DefaultJsonMaxDepth,
    };
    private readonly YooKassaOptions _options = options.Value;
    private readonly YooKassaApiClient _apiClient = new(httpClient, options.Value);

    public string Provider => BillingProviderNames.YooKassa;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.ProviderNotConfigured(Provider));
        }

        string amount = ResolveAmount(request.Plan);
        Result<YooKassaPayment> paymentResponse = await _apiClient.SendAsync<YooKassaPayment>(
            HttpMethod.Post,
            "payments",
            new CreatePaymentRequest(
                new AmountRequest(amount, _options.Currency),
                Capture: true,
                new ConfirmationRequest("redirect", _options.ReturnUrl),
                SavePaymentMethod: true,
                BuildDescription(request.Plan),
                new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                }),
                request.IdempotencyKey,
            cancellationToken).ConfigureAwait(false);
        if (paymentResponse.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(paymentResponse.Error);
        }

        YooKassaPayment payment = paymentResponse.Value;
        string? confirmationUrl = payment.Confirmation?.ConfirmationUrl;
        if (string.IsNullOrWhiteSpace(payment.Id) ||
            !BillingUrlValidator.IsAbsoluteHttps(confirmationUrl)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                BillingErrors.ProviderOperationFailed(Provider, "YooKassa payment identifier or confirmation URL is invalid."));
        }

        return Result.Success(new BillingCheckoutSessionModel(
            payment.Id,
            confirmationUrl!,
            request.UserId.ToString(),
            amount,
            request.Plan));
    }

    public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<BillingPortalSessionModel>(
            BillingErrors.ProviderOperationFailed(Provider, "YooKassa does not provide a hosted customer portal.")));

    public async Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderNotConfigured(Provider));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId)) {
            return Result.Failure<BillingRecurringPaymentModel>(Errors.Validation.Required(nameof(request.PaymentMethodId)));
        }

        string amount = ResolveAmount(request.Plan);
        Result<YooKassaPayment> paymentResponse = await _apiClient.SendAsync<YooKassaPayment>(
            HttpMethod.Post,
            "payments",
            new CreateRecurringPaymentRequest(
                new AmountRequest(amount, _options.Currency),
                Capture: true,
                request.PaymentMethodId,
                BuildDescription(request.Plan),
                new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                    ["renewal"] = "true",
                    ["renewal_period_start"] = request.CurrentPeriodEndUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                }),
            request.IdempotenceKey,
            cancellationToken).ConfigureAwait(false);
        if (paymentResponse.IsFailure) {
            return Result.Failure<BillingRecurringPaymentModel>(paymentResponse.Error);
        }

        return MapRecurringPayment(paymentResponse.Value, request);
    }

    public async Task<Result<BillingRecurringPaymentModel>> GetRecurringPaymentAsync(
        string paymentId,
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForWebhook()) {
            return Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderNotConfigured(Provider));
        }
        if (!IsValidPaymentId(paymentId)) {
            return Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(Provider, "Invalid payment identifier."));
        }
        Result<YooKassaPayment> response = await FetchPaymentAsync(paymentId, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure) {
            return Result.Failure<BillingRecurringPaymentModel>(response.Error);
        }
        return string.Equals(response.Value.Id, paymentId, StringComparison.Ordinal)
            ? MapRecurringPayment(response.Value, request)
            : Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(Provider, "Payment verification returned a different payment."));
    }

    private Result<BillingRecurringPaymentModel> MapRecurringPayment(YooKassaPayment payment, BillingRecurringPaymentRequestModel request) {
        if (string.IsNullOrWhiteSpace(payment.Id)) {
            return Result.Failure<BillingRecurringPaymentModel>(
                BillingErrors.ProviderOperationFailed(Provider, "YooKassa payment identifier is missing."));
        }

        string? status = payment.Status?.ToLowerInvariant() switch {
            "succeeded" when payment.Paid => "active",
            "pending" or "waiting_for_capture" => "pending",
            "canceled" => "canceled",
            _ => null,
        };
        if (status is null) {
            return Result.Failure<BillingRecurringPaymentModel>(BillingErrors.ProviderOperationFailed(Provider, "Unexpected payment state."));
        }
        DateTime? periodStart = string.Equals(status, "active", StringComparison.Ordinal) ? ReadRenewalPeriodStart(payment.Metadata) ?? request.CurrentPeriodEndUtc ?? payment.CapturedAt ?? payment.CreatedAt
            : null;
        DateTime? periodEnd = ResolvePeriodEnd(periodStart, request.Plan);

        return Result.Success(new BillingRecurringPaymentModel(
            payment.Id,
            payment.PaymentMethod?.Id ?? request.PaymentMethodId,
            payment.Amount?.Value ?? ResolveAmount(request.Plan),
            request.Plan,
            status,
            periodStart,
            periodEnd,
            $"yookassa-renewal:{payment.Id}:{payment.Status}",
            ParseAmount(payment.Amount?.Value),
            payment.Amount?.Currency ?? _options.Currency,
            JsonSerializer.Serialize(payment, JsonOptions),
            payment.CapturedAt ?? payment.CreatedAt));
    }

    public async Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(payload)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload)));
        }

        if (!IsConfiguredForWebhook()) {
            return Result.Failure<BillingWebhookEventModel?>(BillingErrors.ProviderNotConfigured(Provider));
        }

        YooKassaNotification? notification;
        try {
            notification = JsonSerializer.Deserialize<YooKassaNotification>(payload, JsonOptions);
        } catch (JsonException) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.WebhookValidationFailed("YooKassa webhook payload is invalid."));
        }

        if (notification?.Object?.Id is null ||
            string.IsNullOrWhiteSpace(notification.Event) ||
            !notification.Event.StartsWith("payment.", StringComparison.OrdinalIgnoreCase)) {
            return Result.Success<BillingWebhookEventModel?>(value: null);
        }

        if (!IsValidPaymentId(notification.Object.Id)) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.WebhookValidationFailed("YooKassa payment id is invalid."));
        }

        Result<YooKassaPayment> paymentResult = await FetchPaymentAsync(notification.Object.Id, cancellationToken).ConfigureAwait(false);
        if (paymentResult.IsFailure) {
            return Result.Failure<BillingWebhookEventModel?>(paymentResult.Error);
        }

        YooKassaPayment payment = paymentResult.Value;
        if (!string.Equals(payment.Id, notification.Object.Id, StringComparison.Ordinal)) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.WebhookValidationFailed("YooKassa payment verification returned a different payment."));
        }

        // An unfinished payment is not a subscription cancellation. Renewal polling or a final webhook will resolve it.
        if (payment.Status is "pending" or "waiting_for_capture") {
            return Result.Success<BillingWebhookEventModel?>(value: null);
        }

        if (payment.Status is not "canceled" && !(payment.Status is "succeeded" && payment.Paid)) {
            return Result.Failure<BillingWebhookEventModel?>(BillingErrors.WebhookValidationFailed("Unexpected payment state."));
        }

        return Result.Success<BillingWebhookEventModel?>(CreateWebhookEvent(payment));
    }

    private static BillingWebhookEventModel CreateWebhookEvent(YooKassaPayment payment) {
        IReadOnlyDictionary<string, string>? metadata = payment.Metadata;
        Guid? userId = ParseUserId(ReadMetadata(metadata, "user_id"));
        string? plan = ReadMetadata(metadata, "plan");
        bool isRenewal = string.Equals(ReadMetadata(metadata, "renewal"), "true", StringComparison.OrdinalIgnoreCase);
        // Legacy renewals without the anchor are resolved from the stored payment/subscription by the application.
        DateTime? periodStart = isRenewal ? ReadRenewalPeriodStart(metadata) : payment.CapturedAt ?? payment.CreatedAt;
        DateTime? periodEnd = ResolvePeriodEnd(periodStart, plan);
        string verifiedEventType = ResolveVerifiedEventType(payment);
        string status = MapStatus(payment);
        string? paymentMethodId = payment.PaymentMethod?.Id;

        return new BillingWebhookEventModel(
            $"{verifiedEventType}:{payment.Id}:{payment.Status}",
            verifiedEventType,
            userId?.ToString() ?? ReadMetadata(metadata, "user_id") ?? payment.Id,
            payment.Id,
            paymentMethodId ?? payment.Id,
            payment.Amount?.Value,
            plan,
            status,
            string.Equals(status, "active", StringComparison.Ordinal) ? periodStart : null,
            string.Equals(status, "active", StringComparison.Ordinal) ? periodEnd : null,
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            ParseAmount(payment.Amount?.Value),
            payment.Amount?.Currency,
            JsonSerializer.Serialize(payment, JsonOptions),
            userId,
            payment.CapturedAt ?? payment.CreatedAt,
            IsAuthoritativeSnapshot: true,
            IsRenewal: isRenewal);
    }

    private static DateTime? ReadRenewalPeriodStart(IReadOnlyDictionary<string, string>? metadata) =>
        DateTime.TryParse(ReadMetadata(metadata, "renewal_period_start"), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out DateTime anchor) && anchor.Kind != DateTimeKind.Unspecified
            ? anchor.ToUniversalTime()
            : null;

    private async Task<Result<YooKassaPayment>> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken) {
        return await _apiClient.SendAsync<YooKassaPayment>(HttpMethod.Get, $"payments/{paymentId}", body: null, idempotenceKey: null, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidPaymentId(string value) {
        if (value.Length is 0 or > MaximumPaymentIdLength) {
            return false;
        }

        return value.All(static character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
    }

    private bool IsConfiguredForCheckout() =>
        YooKassaOptions.HasValidCheckoutConfiguration(_options);

    private bool IsConfiguredForWebhook() =>
        !string.IsNullOrWhiteSpace(_options.ShopId) &&
        !string.IsNullOrWhiteSpace(_options.SecretKey) &&
        !string.IsNullOrWhiteSpace(_options.ApiBaseUrl);

    private string ResolveAmount(string plan) {
        return plan switch {
            "monthly" => NormalizeAmount(_options.PremiumMonthlyAmount),
            "yearly" => NormalizeAmount(_options.PremiumYearlyAmount),
            _ => throw new InvalidOperationException($"Unsupported billing plan '{plan}'."),
        };
    }

    private string BuildDescription(string plan) =>
        $"{_options.Description.Trim()} ({plan})";

    private static string ResolveVerifiedEventType(YooKassaPayment payment) =>
        string.Equals(payment.Status, "succeeded", StringComparison.OrdinalIgnoreCase) && payment.Paid
            ? "payment.succeeded"
            : "payment.canceled";

    private static string MapStatus(YooKassaPayment payment) {
        if (string.Equals(payment.Status, "succeeded", StringComparison.OrdinalIgnoreCase) &&
            payment.Paid) {
            return "active";
        }

        return "canceled";
    }

    private static DateTime? ResolvePeriodEnd(DateTime? periodStart, string? plan) {
        if (!periodStart.HasValue || string.IsNullOrWhiteSpace(plan)) {
            return null;
        }

        return plan.Trim().ToLowerInvariant() switch {
            "monthly" => periodStart.Value.AddMonths(1),
            "yearly" => periodStart.Value.AddYears(1),
            _ => null,
        };
    }

    private static string? ReadMetadata(IReadOnlyDictionary<string, string>? metadata, string key) {
        if (metadata is null || !metadata.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim();
    }

    private static Guid? ParseUserId(string? value) {
        return Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty
            ? parsed
            : null;
    }

    private static decimal? ParseAmount(string? value) {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
            ? amount
            : null;
    }

    private static string NormalizeAmount(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture).ToString("0.00", CultureInfo.InvariantCulture);

    private sealed record CreatePaymentRequest(
        [property: JsonPropertyName("amount")] AmountRequest Amount,
        [property: JsonPropertyName("capture")] bool Capture,
        [property: JsonPropertyName("confirmation")] ConfirmationRequest Confirmation,
        [property: JsonPropertyName("save_payment_method")] bool SavePaymentMethod,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata);

    private sealed record CreateRecurringPaymentRequest(
        [property: JsonPropertyName("amount")] AmountRequest Amount,
        [property: JsonPropertyName("capture")] bool Capture,
        [property: JsonPropertyName("payment_method_id")] string PaymentMethodId,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string> Metadata);

    private sealed record AmountRequest(
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("currency")] string Currency);

    private sealed record ConfirmationRequest(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("return_url")] string ReturnUrl);

    private sealed record YooKassaNotification(
        [property: JsonPropertyName("event")] string Event,
        [property: JsonPropertyName("object")] YooKassaPayment? Object);

    private sealed record YooKassaPayment(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("paid")] bool Paid,
        [property: JsonPropertyName("amount")] AmountResponse? Amount,
        [property: JsonPropertyName("confirmation")] ConfirmationResponse? Confirmation,
        [property: JsonPropertyName("payment_method")] PaymentMethodResponse? PaymentMethod,
        [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string>? Metadata,
        [property: JsonPropertyName("created_at")] DateTime? CreatedAt,
        [property: JsonPropertyName("captured_at")] DateTime? CapturedAt);

    private sealed record AmountResponse(
        [property: JsonPropertyName("value")] string? Value,
        [property: JsonPropertyName("currency")] string? Currency);

    private sealed record ConfirmationResponse(
        [property: JsonPropertyName("confirmation_url")] string? ConfirmationUrl);

    private sealed record PaymentMethodResponse(
        [property: JsonPropertyName("id")] string? Id);
}
