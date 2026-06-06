using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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

        string? customerId = request.ExistingCustomerId;
        if (string.IsNullOrWhiteSpace(customerId)) {
            Result<string> customerResult = await CreateCustomerAsync(request, cancellationToken).ConfigureAwait(false);
            if (customerResult.IsFailure) {
                return Result.Failure<BillingCheckoutSessionModel>(customerResult.Error);
            }

            customerId = customerResult.Value;
        }

        string priceId = ResolvePriceId(request.Plan);
        Result<CreateTransactionResponse> transactionResponse = await SendAsync<CreateTransactionResponse>(
            HttpMethod.Post,
            "transactions",
            new CreateTransactionRequest(
                [
                    new TransactionItemRequest(priceId, 1),
                ],
                customerId,
                "automatic",
                new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                },
                new TransactionCheckoutRequest(_options.CheckoutUrl)),
            cancellationToken).ConfigureAwait(false);
        if (transactionResponse.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(transactionResponse.Error);
        }

        CreateTransactionResponse transaction = transactionResponse.Value;
        if (string.IsNullOrWhiteSpace(transaction.Checkout?.Url)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle transaction checkout URL is missing."));
        }

        return Result.Success(new BillingCheckoutSessionModel(
            transaction.Id,
            transaction.Checkout.Url,
            customerId,
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

        Result<CreateCustomerPortalSessionResponse> sessionResponse = await SendAsync<CreateCustomerPortalSessionResponse>(
            HttpMethod.Post,
            $"customers/{request.CustomerId}/portal-sessions",
            new { },
            cancellationToken).ConfigureAwait(false);
        if (sessionResponse.IsFailure) {
            return Result.Failure<BillingPortalSessionModel>(sessionResponse.Error);
        }

        string? url = sessionResponse.Value.Urls?.General?.Overview;
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

        if (!TryVerifySignature(payload, signatureHeader, out string signatureError)) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed(signatureError)));
        }

        try {
            using var document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            string? eventType = root.GetProperty("event_type").GetString();
            if (string.IsNullOrWhiteSpace(eventType) ||
                !eventType.StartsWith("subscription.", StringComparison.OrdinalIgnoreCase)) {
                return Task.FromResult(Result.Success<BillingWebhookEventModel?>(value: null));
            }

            JsonElement data = root.GetProperty("data");
            string? subscriptionId = GetString(data, "id");
            string? customerId = GetString(data, "customer_id");
            if (string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(customerId)) {
                return Task.FromResult(Result.Success<BillingWebhookEventModel?>(value: null));
            }

            return Task.FromResult(Result.Success<BillingWebhookEventModel?>(CreateWebhookEvent(root, data, eventType, customerId, subscriptionId)));
        } catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed(ex.Message)));
        }
    }

    private BillingWebhookEventModel CreateWebhookEvent(
        JsonElement root,
        JsonElement data,
        string eventType,
        string customerId,
        string subscriptionId) {
        string? externalPriceId = GetFirstItemPriceId(data);
        JsonElement customData = data.TryGetProperty("custom_data", out JsonElement customDataElement) ? customDataElement : default;
        JsonElement currentBillingPeriod = data.TryGetProperty("current_billing_period", out JsonElement billingPeriodElement)
            ? billingPeriodElement
            : default;
        JsonElement trialDates = GetFirstTrialDates(data);
        JsonElement scheduledChange = data.TryGetProperty("scheduled_change", out JsonElement scheduledChangeElement)
            ? scheduledChangeElement
            : default;

        return new BillingWebhookEventModel(
            root.GetProperty("event_id").GetString() ?? string.Empty,
            eventType,
            customerId,
            subscriptionId,
            ExternalPaymentMethodId: null,
            externalPriceId,
            ResolvePlan(externalPriceId) ?? GetString(customData, "plan"),
            GetString(data, "status") ?? string.Empty,
            ParseDateTime(currentBillingPeriod, "starts_at"),
            ParseDateTime(currentBillingPeriod, "ends_at"),
            string.Equals(GetString(scheduledChange, "action"), "cancel", StringComparison.OrdinalIgnoreCase),
            ParseDateTime(data, "canceled_at"),
            ParseDateTime(trialDates, "starts_at"),
            ParseDateTime(trialDates, "ends_at") ?? ParseDateTime(data, "next_billed_at"),
            Amount: null,
            Currency: null,
            ProviderMetadataJson: null,
            ParseUserId(customData));
    }

    private async Task<Result<string>> CreateCustomerAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken) {
        Result<CreateCustomerResponse> customerResponse = await SendAsync<CreateCustomerResponse>(
            HttpMethod.Post,
            "customers",
            new CreateCustomerRequest(
                request.Email,
                Name: null,
                new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                }),
            cancellationToken).ConfigureAwait(false);
        return customerResponse.IsFailure ? Result.Failure<string>(customerResponse.Error) : Result.Success(customerResponse.Value.Id);
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

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            string error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                Provider,
                string.Create(CultureInfo.InvariantCulture, $"{(int)response.StatusCode} {response.ReasonPhrase}: {error}").Trim()));
        }

        PaddleEnvelope<TResponse>? envelope = await response.Content.ReadFromJsonAsync<PaddleEnvelope<TResponse>>(JsonOptions, cancellationToken).ConfigureAwait(false);
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

        string[] parts = signatureHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? timestamp = parts
            .Select(static part => part.Split('=', 2))
            .FirstOrDefault(static values => values.Length == 2 && string.Equals(values[0], "ts", StringComparison.OrdinalIgnoreCase))?[1];
        string[] signatures = [.. parts
            .Select(static part => part.Split('=', 2))
            .Where(static values => values.Length == 2 && string.Equals(values[0], "h1", StringComparison.OrdinalIgnoreCase))
            .Select(static values => values[1])
            .Where(static value => !string.IsNullOrWhiteSpace(value))];

        if (string.IsNullOrWhiteSpace(timestamp) || signatures.Length == 0) {
            error = "Paddle-Signature header is malformed.";
            return false;
        }

        string signedPayload = $"{timestamp}:{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecretKey));
        string computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        byte[] computedBytes = Encoding.UTF8.GetBytes(computedHash);

        foreach (string candidate in signatures) {
            byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate.Trim().ToLowerInvariant());
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
        if (!data.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) {
            return default;
        }

        JsonElement firstItem = items[0];
        return !firstItem.TryGetProperty("trial_dates", out JsonElement trialDates) ? default : trialDates;
    }

    private static string? GetFirstItemPriceId(JsonElement data) {
        if (!data.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) {
            return null;
        }

        JsonElement firstItem = items[0];
        return !firstItem.TryGetProperty("price", out JsonElement price) ? null : GetString(price, "id");
    }

    private static string? GetString(JsonElement element, string propertyName) {
        if (element.ValueKind == JsonValueKind.Undefined ||
            element.ValueKind == JsonValueKind.Null ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind == JsonValueKind.Null) {
            return null;
        }

        return property.GetString();
    }

    private static DateTime? ParseDateTime(JsonElement element, string propertyName) {
        string? value = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    private static Guid? ParseUserId(JsonElement customData) {
        string? rawUserId = GetString(customData, "user_id");
        return Guid.TryParse(rawUserId, out Guid parsed) && parsed != Guid.Empty
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
