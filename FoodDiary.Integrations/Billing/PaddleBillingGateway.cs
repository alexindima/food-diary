using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class PaddleBillingGateway(
    HttpClient httpClient,
    IOptions<PaddleOptions> options)
    : IBillingProviderGateway {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PaddleOptions _options = options.Value;

    public string Provider => BillingProviderNames.Paddle;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        ConfigureClient();

        var customerId = request.ExistingCustomerId;
        if (string.IsNullOrWhiteSpace(customerId)) {
            var customerResult = await CreateCustomerAsync(request, cancellationToken);
            if (customerResult.IsFailure) {
                return Result.Failure<BillingCheckoutSessionModel>(customerResult.Error);
            }

            customerId = customerResult.Value;
        }

        var priceId = ResolvePriceId(request.Plan);
        var transactionResponse = await SendAsync<CreateTransactionResponse>(
            HttpMethod.Post,
            "transactions",
            new CreateTransactionRequest(
                [
                    new TransactionItemRequest(priceId, 1),
                ],
                customerId,
                "automatic",
                new Dictionary<string, string> {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                },
                new TransactionCheckoutRequest(_options.CheckoutUrl)),
            cancellationToken);
        if (transactionResponse.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(transactionResponse.Error);
        }

        var transaction = transactionResponse.Value;
        if (string.IsNullOrWhiteSpace(transaction.Checkout?.Url)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle transaction checkout URL is missing."));
        }

        return Result.Success(new BillingCheckoutSessionModel(
            transaction.Id,
            transaction.Checkout.Url,
            customerId!,
            priceId,
            request.Plan));
    }

    public async Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForPortal()) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        ConfigureClient();

        var sessionResponse = await SendAsync<CreateCustomerPortalSessionResponse>(
            HttpMethod.Post,
            $"customers/{request.CustomerId}/portal-sessions",
            new { },
            cancellationToken);
        if (sessionResponse.IsFailure) {
            return Result.Failure<BillingPortalSessionModel>(sessionResponse.Error);
        }

        var url = sessionResponse.Value.Urls?.General?.Overview;
        if (string.IsNullOrWhiteSpace(url)) {
            return Result.Failure<BillingPortalSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle customer portal URL is missing."));
        }

        return Result.Success(new BillingPortalSessionModel(url));
    }

    public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(payload)) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload))));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(signatureHeader))));
        }

        if (!IsConfiguredForWebhook()) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(Errors.Billing.ProviderNotConfigured(Provider)));
        }

        if (!TryVerifySignature(payload, signatureHeader, out var signatureError)) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed(signatureError)));
        }

        try {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventType = root.GetProperty("event_type").GetString();
            if (string.IsNullOrWhiteSpace(eventType) ||
                !eventType.StartsWith("subscription.", StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(Result.Success<BillingWebhookEventModel?>(null));
            }

            var data = root.GetProperty("data");
            var subscriptionId = GetString(data, "id");
            var customerId = GetString(data, "customer_id");
            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(customerId)) {
                return Task.FromResult(Result.Success<BillingWebhookEventModel?>(null));
            }

            var externalPriceId = GetFirstItemPriceId(data);
            var customData = data.TryGetProperty("custom_data", out var customDataElement) ? customDataElement : default;
            var currentBillingPeriod = data.TryGetProperty("current_billing_period", out var billingPeriodElement)
                ? billingPeriodElement
                : default;
            var trialDates = GetFirstTrialDates(data);
            var scheduledChange = data.TryGetProperty("scheduled_change", out var scheduledChangeElement)
                ? scheduledChangeElement
                : default;

            var model = new BillingWebhookEventModel(
                root.GetProperty("event_id").GetString() ?? string.Empty,
                eventType,
                customerId,
                subscriptionId,
                externalPriceId,
                ResolvePlan(externalPriceId) ?? GetString(customData, "plan"),
                GetString(data, "status") ?? string.Empty,
                ParseDateTime(currentBillingPeriod, "starts_at"),
                ParseDateTime(currentBillingPeriod, "ends_at"),
                string.Equals(GetString(scheduledChange, "action"), "cancel", StringComparison.OrdinalIgnoreCase),
                ParseDateTime(data, "canceled_at"),
                ParseDateTime(trialDates, "starts_at"),
                ParseDateTime(trialDates, "ends_at") ?? ParseDateTime(data, "next_billed_at"),
                ParseUserId(customData));

            return Task.FromResult(Result.Success<BillingWebhookEventModel?>(model));
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed(ex.Message)));
        }
    }

    private async Task<Result<string>> CreateCustomerAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken) {
        var customerResponse = await SendAsync<CreateCustomerResponse>(
            HttpMethod.Post,
            "customers",
            new CreateCustomerRequest(
                request.Email,
                null,
                new Dictionary<string, string> {
                    ["user_id"] = request.UserId.ToString(),
                }),
            cancellationToken);
        if (customerResponse.IsFailure) {
            return Result.Failure<string>(customerResponse.Error);
        }

        return Result.Success(customerResponse.Value.Id);
    }

    private async Task<Result<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
        where TResponse : class {
        using var request = new HttpRequestMessage(method, path);
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

        var envelope = await response.Content.ReadFromJsonAsync<PaddleEnvelope<TResponse>>(JsonOptions, cancellationToken);
        if (envelope?.Data is null) {
            return Result.Failure<TResponse>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle returned an empty response."));
        }

        return Result.Success(envelope.Data);
    }

    private void ConfigureClient() {
        httpClient.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private bool TryVerifySignature(string payload, string signatureHeader, out string error) {
        error = string.Empty;

        var parts = signatureHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var timestamp = parts
            .Select(static part => part.Split('=', 2))
            .FirstOrDefault(static values => values.Length == 2 && string.Equals(values[0], "ts", StringComparison.OrdinalIgnoreCase))?[1];
        var signatures = parts
            .Select(static part => part.Split('=', 2))
            .Where(static values => values.Length == 2 && string.Equals(values[0], "h1", StringComparison.OrdinalIgnoreCase))
            .Select(static values => values[1])
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (string.IsNullOrWhiteSpace(timestamp) || signatures.Length == 0) {
            error = "Paddle-Signature header is malformed.";
            return false;
        }

        var signedPayload = $"{timestamp}:{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecretKey));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);

        foreach (var candidate in signatures) {
            var candidateBytes = Encoding.UTF8.GetBytes(candidate.Trim().ToLowerInvariant());
            if (candidateBytes.Length == computedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(candidateBytes, computedBytes)) {
                return true;
            }
        }

        error = "Paddle webhook signature is invalid.";
        return false;
    }

    private bool IsConfiguredForCheckout() =>
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ApiBaseUrl) &&
        !string.IsNullOrWhiteSpace(_options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(_options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(_options.CheckoutUrl, UriKind.Absolute);

    private bool IsConfiguredForPortal() =>
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ApiBaseUrl);

    private bool IsConfiguredForWebhook() =>
        !string.IsNullOrWhiteSpace(_options.WebhookSecretKey) &&
        !string.IsNullOrWhiteSpace(_options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(_options.PremiumYearlyPriceId);

    private string ResolvePriceId(string plan) {
        return plan switch {
            "monthly" => _options.PremiumMonthlyPriceId,
            "yearly" => _options.PremiumYearlyPriceId,
            _ => throw new InvalidOperationException($"Unsupported billing plan '{plan}'."),
        };
    }

    private string? ResolvePlan(string? priceId) {
        if (string.IsNullOrWhiteSpace(priceId)) {
            return null;
        }

        if (string.Equals(priceId, _options.PremiumMonthlyPriceId, StringComparison.Ordinal)) {
            return "monthly";
        }

        if (string.Equals(priceId, _options.PremiumYearlyPriceId, StringComparison.Ordinal)) {
            return "yearly";
        }

        return null;
    }

    private static JsonElement GetFirstTrialDates(JsonElement data) {
        if (!data.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) {
            return default;
        }

        var firstItem = items[0];
        if (!firstItem.TryGetProperty("trial_dates", out var trialDates)) {
            return default;
        }

        return trialDates;
    }

    private static string? GetFirstItemPriceId(JsonElement data) {
        if (!data.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) {
            return null;
        }

        var firstItem = items[0];
        if (!firstItem.TryGetProperty("price", out var price)) {
            return null;
        }

        return GetString(price, "id");
    }

    private static string? GetString(JsonElement element, string propertyName) {
        if (element.ValueKind == JsonValueKind.Undefined ||
            element.ValueKind == JsonValueKind.Null ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return property.GetString();
    }

    private static DateTime? ParseDateTime(JsonElement element, string propertyName) {
        var value = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    private static Guid? ParseUserId(JsonElement customData) {
        var rawUserId = GetString(customData, "user_id");
        return Guid.TryParse(rawUserId, out var parsed) && parsed != Guid.Empty
            ? parsed
            : null;
    }

    private sealed record PaddleEnvelope<T>(
        [property: JsonPropertyName("data")] T? Data);

    private sealed record CreateCustomerRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("custom_data")] IReadOnlyDictionary<string, string>? CustomData);

    private sealed record CreateCustomerResponse(
        [property: JsonPropertyName("id")] string Id);

    private sealed record CreateTransactionRequest(
        [property: JsonPropertyName("items")] IReadOnlyList<TransactionItemRequest> Items,
        [property: JsonPropertyName("customer_id")] string CustomerId,
        [property: JsonPropertyName("collection_mode")] string CollectionMode,
        [property: JsonPropertyName("custom_data")] IReadOnlyDictionary<string, string>? CustomData,
        [property: JsonPropertyName("checkout")] TransactionCheckoutRequest Checkout);

    private sealed record TransactionItemRequest(
        [property: JsonPropertyName("price_id")] string PriceId,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record TransactionCheckoutRequest(
        [property: JsonPropertyName("url")] string Url);

    private sealed record CreateTransactionResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("checkout")] TransactionCheckoutResponse? Checkout);

    private sealed record TransactionCheckoutResponse(
        [property: JsonPropertyName("url")] string? Url);

    private sealed record CreateCustomerPortalSessionResponse(
        [property: JsonPropertyName("urls")] CustomerPortalUrls? Urls);

    private sealed record CustomerPortalUrls(
        [property: JsonPropertyName("general")] CustomerPortalGeneralUrls? General);

    private sealed record CustomerPortalGeneralUrls(
        [property: JsonPropertyName("overview")] string? Overview);
}
