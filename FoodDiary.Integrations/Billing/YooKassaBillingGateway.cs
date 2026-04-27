using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class YooKassaBillingGateway(
    HttpClient httpClient,
    IOptions<YooKassaOptions> options)
    : IBillingProviderGateway, IBillingRecurringProviderGateway {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly YooKassaOptions _options = options.Value;

    public string Provider => BillingProviderNames.YooKassa;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        ConfigureClient();

        var amount = ResolveAmount(request.Plan);
        var paymentResponse = await SendAsync<YooKassaPayment>(
            HttpMethod.Post,
            "payments",
            new CreatePaymentRequest(
                new AmountRequest(amount, _options.Currency),
                true,
                new ConfirmationRequest("redirect", _options.ReturnUrl),
                true,
                BuildDescription(request.Plan),
                new Dictionary<string, string> {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                }),
            cancellationToken);
        if (paymentResponse.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(paymentResponse.Error);
        }

        var payment = paymentResponse.Value;
        if (string.IsNullOrWhiteSpace(payment.Confirmation?.ConfirmationUrl)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "YooKassa confirmation URL is missing."));
        }

        return Result.Success(new BillingCheckoutSessionModel(
            payment.Id,
            payment.Confirmation.ConfirmationUrl,
            request.UserId.ToString(),
            amount,
            request.Plan));
    }

    public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure<BillingPortalSessionModel>(
            Errors.Billing.ProviderOperationFailed(Provider, "YooKassa does not provide a hosted customer portal.")));

    public async Task<Result<BillingRecurringPaymentModel>> CreateRecurringPaymentAsync(
        BillingRecurringPaymentRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingRecurringPaymentModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId)) {
            return Result.Failure<BillingRecurringPaymentModel>(Errors.Validation.Required(nameof(request.PaymentMethodId)));
        }

        ConfigureClient();

        var amount = ResolveAmount(request.Plan);
        var paymentResponse = await SendAsync<YooKassaPayment>(
            HttpMethod.Post,
            "payments",
            new CreateRecurringPaymentRequest(
                new AmountRequest(amount, _options.Currency),
                true,
                request.PaymentMethodId,
                BuildDescription(request.Plan),
                new Dictionary<string, string> {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                    ["renewal"] = "true",
                }),
            cancellationToken);
        if (paymentResponse.IsFailure) {
            return Result.Failure<BillingRecurringPaymentModel>(paymentResponse.Error);
        }

        var payment = paymentResponse.Value;
        var status = payment.Paid && string.Equals(payment.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
            ? "active"
            : "past_due";
        var periodStart = status == "active"
            ? request.CurrentPeriodEndUtc ?? payment.CapturedAt ?? payment.CreatedAt
            : null;
        var periodEnd = ResolvePeriodEnd(periodStart, request.Plan);

        return Result.Success(new BillingRecurringPaymentModel(
            payment.Id,
            payment.PaymentMethod?.Id ?? request.PaymentMethodId,
            payment.Amount?.Value ?? amount,
            request.Plan,
            status,
            periodStart,
            status == "active" ? periodEnd : request.CurrentPeriodEndUtc,
            $"yookassa-renewal:{payment.Id}:{payment.Status}",
            ParseAmount(payment.Amount?.Value),
            payment.Amount?.Currency ?? _options.Currency,
            JsonSerializer.Serialize(payment, JsonOptions)));
    }

    public async Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(payload)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload)));
        }

        if (!IsConfiguredForWebhook()) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        YooKassaNotification? notification;
        try {
            notification = JsonSerializer.Deserialize<YooKassaNotification>(payload, JsonOptions);
        } catch (JsonException ex) {
            return Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed(ex.Message));
        }

        if (notification?.Object?.Id is null ||
            string.IsNullOrWhiteSpace(notification.Event) ||
            !notification.Event.StartsWith("payment.", StringComparison.OrdinalIgnoreCase)) {
            return Result.Success<BillingWebhookEventModel?>(null);
        }

        var paymentResult = await FetchPaymentAsync(notification.Object.Id, cancellationToken);
        if (paymentResult.IsFailure) {
            return Result.Failure<BillingWebhookEventModel?>(paymentResult.Error);
        }

        var payment = paymentResult.Value;
        if (!string.Equals(payment.Id, notification.Object.Id, StringComparison.Ordinal)) {
            return Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("YooKassa payment verification returned a different payment."));
        }

        var metadata = payment.Metadata;
        var userId = ParseUserId(ReadMetadata(metadata, "user_id"));
        var plan = ReadMetadata(metadata, "plan");
        var periodStart = payment.CapturedAt ?? payment.CreatedAt;
        var periodEnd = ResolvePeriodEnd(periodStart, plan);
        var status = MapStatus(notification.Event, payment);
        var paymentMethodId = payment.PaymentMethod?.Id;

        return Result.Success<BillingWebhookEventModel?>(new BillingWebhookEventModel(
            $"{notification.Event}:{payment.Id}:{payment.Status}",
            notification.Event,
            userId?.ToString() ?? ReadMetadata(metadata, "user_id") ?? payment.Id,
            payment.Id,
            paymentMethodId ?? payment.Id,
            payment.Amount?.Value,
            plan,
            status,
            periodStart,
            status == "active" ? periodEnd : null,
            false,
            null,
            null,
            null,
            ParseAmount(payment.Amount?.Value),
            payment.Amount?.Currency,
            JsonSerializer.Serialize(payment, JsonOptions),
            userId));
    }

    private async Task<Result<YooKassaPayment>> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken) {
        ConfigureClient();
        return await SendAsync<YooKassaPayment>(HttpMethod.Get, $"payments/{paymentId}", null, cancellationToken);
    }

    private async Task<Result<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
        where TResponse : class {
        using var request = new HttpRequestMessage(method, path);
        if (method != HttpMethod.Get) {
            request.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString("N"));
        }
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                Provider,
                $"{(int)response.StatusCode} {response.ReasonPhrase}: {error}".Trim()));
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        if (result is null) {
            return Result.Failure<TResponse>(
                Errors.Billing.ProviderOperationFailed(Provider, "YooKassa returned an empty response."));
        }

        return Result.Success(result);
    }

    private void ConfigureClient() {
        httpClient.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var authBytes = Encoding.UTF8.GetBytes($"{_options.ShopId}:{_options.SecretKey}");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

    private static string MapStatus(string eventType, YooKassaPayment payment) {
        if (string.Equals(eventType, "payment.succeeded", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(payment.Status, "succeeded", StringComparison.OrdinalIgnoreCase) &&
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
        if (metadata is null || !metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value.Trim();
    }

    private static Guid? ParseUserId(string? value) {
        return Guid.TryParse(value, out var parsed) && parsed != Guid.Empty
            ? parsed
            : null;
    }

    private static decimal? ParseAmount(string? value) {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
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
