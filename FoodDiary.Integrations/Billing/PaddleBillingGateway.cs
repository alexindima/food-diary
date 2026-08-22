using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class PaddleBillingGateway(
    HttpClient httpClient,
    IOptions<PaddleOptions> options,
    TimeProvider? timeProvider = null)
    : IBillingProviderGateway {
    private const int RecoveryPageSize = 200;
    private const int MaximumRecoveryPages = 10;
    private static readonly TimeSpan RecentRecoveryWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaximumRecoveryFutureSkew = TimeSpan.FromMinutes(5);
    private readonly PaddleOptions _options = options.Value;
    private readonly PaddleApiClient _apiClient = new(httpClient, options.Value);
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public string Provider => BillingProviderNames.Paddle;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        try {
            string? customerId = request.ExistingCustomerId;
            if (string.IsNullOrWhiteSpace(customerId)) {
                Result<string?> existingCustomerResult = await FindExistingCustomerIdAsync(request, cancellationToken).ConfigureAwait(false);
                if (existingCustomerResult.IsFailure) {
                    return Result.Failure<BillingCheckoutSessionModel>(existingCustomerResult.Error);
                }

                customerId = existingCustomerResult.Value;
            }
            if (string.IsNullOrWhiteSpace(customerId)) {
                Result<string> customerResult = await CreateCustomerAsync(request, cancellationToken).ConfigureAwait(false);
                if (customerResult.IsFailure) {
                    return Result.Failure<BillingCheckoutSessionModel>(customerResult.Error);
                }

                customerId = customerResult.Value;
            }

            string priceId = ResolvePriceId(request.Plan);
            Result<CreateTransactionResponse> transactionResponse = await GetOrCreateTransactionAsync(
                request,
                customerId,
                priceId,
                cancellationToken).ConfigureAwait(false);
            if (transactionResponse.IsFailure) {
                return Result.Failure<BillingCheckoutSessionModel>(transactionResponse.Error);
            }

            CreateTransactionResponse transaction = transactionResponse.Value;
            string? checkoutUrl = transaction.Checkout?.Url;
            if (string.IsNullOrWhiteSpace(transaction.Id) ||
                !BillingUrlValidator.IsAbsoluteHttps(checkoutUrl)) {
                return Result.Failure<BillingCheckoutSessionModel>(
                    Errors.Billing.ProviderOperationFailed(Provider, "Paddle transaction identifier or checkout URL is invalid."));
            }

            return Result.Success(new BillingCheckoutSessionModel(
                transaction.Id,
                checkoutUrl!,
                customerId,
                priceId,
                request.Plan));
        } catch (Exception exception) when (IsAmbiguousNetworkFailure(exception, cancellationToken)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle request could not be completed."));
        }
    }

    private async Task<Result<CreateTransactionResponse>> GetOrCreateTransactionAsync(
        BillingCheckoutSessionRequestModel request,
        string customerId,
        string priceId,
        CancellationToken cancellationToken) {
        string checkoutReference = CreateCheckoutReference(request);
        Result<CreateTransactionResponse?> recoveryResult = await FindRecoverableTransactionAsync(
            request,
            customerId,
            priceId,
            checkoutReference,
            cancellationToken).ConfigureAwait(false);
        if (recoveryResult.IsFailure) {
            return Result.Failure<CreateTransactionResponse>(recoveryResult.Error);
        }

        CreateTransactionResponse? recoveredTransaction = recoveryResult.Value;
        Result<CreateTransactionResponse> transactionResponse;
        if (recoveredTransaction is not null) {
            transactionResponse = Result.Success(recoveredTransaction);
        } else {
            try {
                transactionResponse = await _apiClient.SendAsync<CreateTransactionResponse>(
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
                            ["checkout_reference"] = checkoutReference,
                        },
                        new TransactionCheckoutRequest(_options.CheckoutUrl)),
                    cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) when (IsAmbiguousNetworkFailure(ex, cancellationToken)) {
                recoveryResult = await FindRecoverableTransactionAsync(
                    request,
                    customerId,
                    priceId,
                    checkoutReference,
                    cancellationToken).ConfigureAwait(false);
                recoveredTransaction = recoveryResult.IsSuccess ? recoveryResult.Value : null;
                transactionResponse = recoveryResult.IsFailure || recoveredTransaction is null
                    ? Result.Failure<CreateTransactionResponse>(Errors.Billing.ProviderOperationFailed(
                        Provider,
                        "Paddle transaction creation result is unknown; retry checkout to recover it safely."))
                    : Result.Success(recoveredTransaction);
            }
        }
        return transactionResponse;
    }

    public async Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForPortal()) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        try {
            Result<CreateCustomerPortalSessionResponse> sessionResponse = await _apiClient.SendAsync<CreateCustomerPortalSessionResponse>(
                HttpMethod.Post,
                $"customers/{Uri.EscapeDataString(request.CustomerId)}/portal-sessions",
                new { },
                cancellationToken).ConfigureAwait(false);
            if (sessionResponse.IsFailure) {
                return Result.Failure<BillingPortalSessionModel>(sessionResponse.Error);
            }

            string? url = sessionResponse.Value.Urls?.General?.Overview;
            if (!BillingUrlValidator.IsAbsoluteHttps(url)) {
                return Result.Failure<BillingPortalSessionModel>(
                    Errors.Billing.ProviderOperationFailed(Provider, "Paddle customer portal URL is invalid."));
            }

            return Result.Success(new BillingPortalSessionModel(url!));
        } catch (Exception exception) when (IsAmbiguousNetworkFailure(exception, cancellationToken)) {
            return Result.Failure<BillingPortalSessionModel>(
                Errors.Billing.ProviderOperationFailed(Provider, "Paddle request could not be completed."));
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
            return Task.FromResult(ParseVerifiedWebhookEvent(payload));
        } catch (JsonException) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook payload is invalid.")));
        } catch (InvalidOperationException) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook price or structure is invalid.")));
        } catch (FormatException) {
            return Task.FromResult(Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook numeric value is invalid.")));
        }
    }

    private Result<BillingWebhookEventModel?> ParseVerifiedWebhookEvent(string payload) {
        using var document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) {
            return Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook payload must be a JSON object."));
        }

        string? eventType = GetString(root, "event_type");
        if (string.IsNullOrWhiteSpace(eventType)) {
            return Result.Success<BillingWebhookEventModel?>(value: null);
        }

        bool isTransactionEvent = eventType.StartsWith("transaction.", StringComparison.OrdinalIgnoreCase);
        bool isAdjustmentEvent = eventType.StartsWith("adjustment.", StringComparison.OrdinalIgnoreCase);
        bool isSubscriptionEvent = eventType.StartsWith("subscription.", StringComparison.OrdinalIgnoreCase);
        if (!isTransactionEvent && !isAdjustmentEvent && !isSubscriptionEvent) {
            return Result.Success<BillingWebhookEventModel?>(value: null);
        }

        if (string.IsNullOrWhiteSpace(GetString(root, "event_id"))) {
            return Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook event identifier is missing."));
        }

        if (!root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object) {
            return Result.Failure<BillingWebhookEventModel?>(
                Errors.Billing.WebhookValidationFailed("Paddle webhook data is missing or invalid."));
        }

        if (isTransactionEvent) {
            return Result.Success(CreateTransactionWebhookEvent(root, data, eventType));
        }

        if (isAdjustmentEvent) {
            return Result.Success(CreateAdjustmentWebhookEvent(root, data, eventType));
        }

        string? subscriptionId = GetString(data, "id");
        string? customerId = GetString(data, "customer_id");
        return string.IsNullOrWhiteSpace(subscriptionId) || string.IsNullOrWhiteSpace(customerId)
            ? Result.Success<BillingWebhookEventModel?>(value: null)
            : Result.Success<BillingWebhookEventModel?>(CreateWebhookEvent(root, data, eventType, customerId, subscriptionId));
    }

    private BillingWebhookEventModel? CreateTransactionWebhookEvent(JsonElement root, JsonElement data, string eventType) {
        string? transactionId = GetString(data, "id");
        string? customerId = GetString(data, "customer_id");
        if (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(customerId)) {
            return null;
        }

        JsonElement customData = data.TryGetProperty("custom_data", out JsonElement customDataElement) ? customDataElement : default;
        JsonElement billingPeriod = data.TryGetProperty("billing_period", out JsonElement billingPeriodElement)
            ? billingPeriodElement
            : default;
        string? currency = GetString(data, "currency_code");
        string? externalPriceId = GetFirstItemPriceId(data);
        JsonElement totals = GetTransactionTotals(data);
        JsonElement payoutTotals = data.TryGetProperty("details", out JsonElement details) &&
            details.TryGetProperty("payout_totals", out JsonElement payoutTotalsElement)
                ? payoutTotalsElement
                : default;
        string? payoutCurrency = GetString(payoutTotals, "currency_code");

        return new BillingWebhookEventModel(
            GetString(root, "event_id") ?? string.Empty,
            eventType,
            customerId,
            GetString(data, "subscription_id"),
            ExternalPaymentMethodId: null,
            externalPriceId,
            ResolvePlan(externalPriceId) ?? GetString(customData, "plan"),
            GetString(data, "status") ?? string.Empty,
            ParseDateTime(billingPeriod, "starts_at"),
            ParseDateTime(billingPeriod, "ends_at"),
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            ParseMoney(data, currency),
            currency,
            ProviderMetadataJson: null,
            ParseUserId(customData),
            ParseDateTime(root, "occurred_at"),
            ExternalPaymentId: transactionId,
            RelatedTransactionId: transactionId,
            FinancialAction: null,
            Quantity: GetTotalQuantity(data),
            UpdatesSubscription: false,
            Tax: ParseMoneyProperty(totals, "tax", currency),
            Fee: ParseMoneyProperty(totals, "fee", currency),
            Earnings: ParseMoneyProperty(totals, "earnings", currency),
            PayoutCurrency: payoutCurrency,
            PayoutEarnings: ParseMoneyProperty(payoutTotals, "earnings", payoutCurrency));
    }

    private static BillingWebhookEventModel? CreateAdjustmentWebhookEvent(JsonElement root, JsonElement data, string eventType) {
        string? adjustmentId = GetString(data, "id");
        string? transactionId = GetString(data, "transaction_id");
        string? customerId = GetString(data, "customer_id");
        if (string.IsNullOrWhiteSpace(adjustmentId) || string.IsNullOrWhiteSpace(transactionId)) {
            return null;
        }

        string? status = GetString(data, "status");
        string? action = GetString(data, "action");
        string? currency = GetString(data, "currency_code");
        JsonElement totals = data.TryGetProperty("totals", out JsonElement totalsElement) ? totalsElement : default;
        JsonElement payoutTotals = data.TryGetProperty("payout_totals", out JsonElement payoutTotalsElement)
            ? payoutTotalsElement
            : default;
        string? payoutCurrency = GetString(payoutTotals, "currency_code");
        decimal? amount = string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase)
            ? ApplyAdjustmentSign(ParseMoney(data, currency), action)
            : null;

        return new BillingWebhookEventModel(
            GetString(root, "event_id") ?? string.Empty,
            eventType,
            customerId ?? string.Empty,
            GetString(data, "subscription_id"),
            ExternalPaymentMethodId: null,
            ExternalPriceId: null,
            Plan: null,
            status ?? string.Empty,
            CurrentPeriodStartUtc: null,
            CurrentPeriodEndUtc: null,
            CancelAtPeriodEnd: false,
            CanceledAtUtc: null,
            TrialStartUtc: null,
            TrialEndUtc: null,
            amount,
            currency,
            ProviderMetadataJson: null,
            UserId: null,
            ParseDateTime(root, "occurred_at"),
            ExternalPaymentId: adjustmentId,
            RelatedTransactionId: transactionId,
            FinancialAction: action,
            Quantity: null,
            UpdatesSubscription: false,
            Tax: ApplyAdjustmentSign(ParseMoneyProperty(totals, "tax", currency), action),
            Fee: ApplyAdjustmentSign(ParseMoneyProperty(totals, "fee", currency), action),
            Earnings: ApplyAdjustmentSign(ParseMoneyProperty(totals, "earnings", currency), action),
            PayoutCurrency: payoutCurrency,
            PayoutEarnings: ApplyAdjustmentSign(ParseMoneyProperty(payoutTotals, "earnings", payoutCurrency), action));
    }

    private BillingWebhookEventModel CreateWebhookEvent(
        JsonElement root,
        JsonElement data,
        string eventType,
        string customerId,
        string subscriptionId) {
        string? externalPriceId = GetFirstItemPriceId(data);
        string plan = ResolvePlan(externalPriceId)
            ?? throw new InvalidOperationException($"Paddle subscription price '{externalPriceId}' is not an approved Premium price.");
        JsonElement customData = data.TryGetProperty("custom_data", out JsonElement customDataElement) ? customDataElement : default;
        JsonElement currentBillingPeriod = data.TryGetProperty("current_billing_period", out JsonElement billingPeriodElement)
            ? billingPeriodElement
            : default;
        JsonElement trialDates = GetFirstTrialDates(data);
        JsonElement scheduledChange = data.TryGetProperty("scheduled_change", out JsonElement scheduledChangeElement)
            ? scheduledChangeElement
            : default;

        return new BillingWebhookEventModel(
            GetString(root, "event_id") ?? string.Empty,
            eventType,
            customerId,
            subscriptionId,
            ExternalPaymentMethodId: null,
            externalPriceId,
            plan,
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
            ParseUserId(customData),
            ParseDateTime(root, "occurred_at"),
            Quantity: GetTotalQuantity(data));
    }

    private async Task<Result<string>> CreateCustomerAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken) {
        Result<CreateCustomerResponse> customerResponse = await _apiClient.SendAsync<CreateCustomerResponse>(
            HttpMethod.Post,
            "customers",
            new CreateCustomerRequest(
                request.Email,
                Name: null,
                new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                }),
            cancellationToken).ConfigureAwait(false);
        if (customerResponse.IsFailure) {
            return Result.Failure<string>(customerResponse.Error);
        }

        return string.IsNullOrWhiteSpace(customerResponse.Value.Id)
            ? Result.Failure<string>(Errors.Billing.ProviderOperationFailed(Provider, "Paddle customer identifier is missing."))
            : Result.Success(customerResponse.Value.Id);
    }

    private async Task<Result<string?>> FindExistingCustomerIdAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken) {
        string? next = $"customers?email={Uri.EscapeDataString(request.Email)}&per_page={RecoveryPageSize}";
        string userId = request.UserId.ToString();
        for (int pageNumber = 0; pageNumber < MaximumRecoveryPages && next is not null; pageNumber++) {
            Result<PaddlePage<CreateCustomerResponse>> pageResult = await _apiClient
                .GetPageAsync<CreateCustomerResponse>(next, cancellationToken)
                .ConfigureAwait(false);
            if (pageResult.IsFailure) {
                return Result.Failure<string?>(pageResult.Error);
            }

            CreateCustomerResponse? customer = pageResult.Value.Items.FirstOrDefault(candidate =>
                candidate.CustomData?.TryGetValue("user_id", out string? value) == true &&
                string.Equals(value, userId, StringComparison.OrdinalIgnoreCase));
            if (customer is not null) {
                return string.IsNullOrWhiteSpace(customer.Id)
                    ? Result.Failure<string?>(
                        Errors.Billing.ProviderOperationFailed(Provider, "Paddle customer identifier is missing."))
                    : Result.Success<string?>(customer.Id);
            }

            next = pageResult.Value.Next;
        }

        return next is null
            ? Result.Success<string?>(value: null)
            : Result.Failure<string?>(Errors.Billing.ProviderOperationFailed(
                Provider,
                "Paddle customer recovery exceeded the safe pagination limit."));
    }

    private async Task<Result<CreateTransactionResponse?>> FindRecoverableTransactionAsync(
        BillingCheckoutSessionRequestModel request,
        string customerId,
        string priceId,
        string checkoutReference,
        CancellationToken cancellationToken) {
        string? next = $"transactions?customer_id={Uri.EscapeDataString(customerId)}&origin=api&per_page={RecoveryPageSize}";
        string userId = request.UserId.ToString();
        CreateTransactionResponse? recentFallback = null;
        for (int pageNumber = 0; pageNumber < MaximumRecoveryPages && next is not null; pageNumber++) {
            Result<PaddlePage<CreateTransactionResponse>> pageResult = await _apiClient
                .GetPageAsync<CreateTransactionResponse>(next, cancellationToken)
                .ConfigureAwait(false);
            if (pageResult.IsFailure) {
                return Result.Failure<CreateTransactionResponse?>(pageResult.Error);
            }

            foreach (CreateTransactionResponse transaction in pageResult.Value.Items.Where(candidate =>
                         IsRecoveryCandidate(candidate, userId, request.Plan))) {
                bool exactReference = transaction.CustomData?.TryGetValue(
                    "checkout_reference",
                    out string? storedReference) == true &&
                    string.Equals(storedReference, checkoutReference, StringComparison.Ordinal);
                if (exactReference) {
                    return IsValidRecoverableTransaction(transaction, priceId)
                        ? Result.Success<CreateTransactionResponse?>(transaction)
                        : InvalidRecoverableTransaction();
                }

                if (!IsRecentRecoveryCandidate(transaction)) {
                    continue;
                }

                if (!TryGetSingleTransactionPriceId(transaction, out string? transactionPriceId)) {
                    return InvalidRecoverableTransaction();
                }

                if (!string.Equals(transactionPriceId, priceId, StringComparison.Ordinal)) {
                    continue;
                }

                if (!HasValidTransactionIdentityAndUrl(transaction)) {
                    return InvalidRecoverableTransaction();
                }

                recentFallback ??= transaction;
            }

            next = pageResult.Value.Next;
        }

        return next is not null
            ? Result.Failure<CreateTransactionResponse?>(Errors.Billing.ProviderOperationFailed(
                Provider,
                "Paddle transaction recovery exceeded the safe pagination limit."))
            : Result.Success(recentFallback);
    }

    private bool IsRecentRecoveryCandidate(CreateTransactionResponse transaction) {
        if (!transaction.CreatedAt.HasValue) {
            return false;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        return transaction.CreatedAt.Value >= now - RecentRecoveryWindow &&
               transaction.CreatedAt.Value <= now + MaximumRecoveryFutureSkew;
    }

    private static bool IsRecoveryCandidate(
        CreateTransactionResponse transaction,
        string userId,
        string plan) =>
        (string.Equals(transaction.Status, "draft", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(transaction.Status, "ready", StringComparison.OrdinalIgnoreCase)) &&
        transaction.CustomData?.TryGetValue("user_id", out string? storedUserId) == true &&
        string.Equals(storedUserId, userId, StringComparison.OrdinalIgnoreCase) &&
        transaction.CustomData.TryGetValue("plan", out string? storedPlan) &&
        string.Equals(storedPlan, plan, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidRecoverableTransaction(
        CreateTransactionResponse transaction,
        string priceId) =>
        HasValidTransactionIdentityAndUrl(transaction) &&
        TryGetSingleTransactionPriceId(transaction, out string? transactionPriceId) &&
        string.Equals(transactionPriceId, priceId, StringComparison.Ordinal);

    private static bool HasValidTransactionIdentityAndUrl(CreateTransactionResponse transaction) =>
        !string.IsNullOrWhiteSpace(transaction.Id) &&
        BillingUrlValidator.IsAbsoluteHttps(transaction.Checkout?.Url);

    private static bool TryGetSingleTransactionPriceId(
        CreateTransactionResponse transaction,
        out string? priceId) {
        priceId = null;
        if (transaction.Items is not { Count: 1 } ||
            transaction.Items[0].Quantity != 1 ||
            string.IsNullOrWhiteSpace(transaction.Items[0].Price?.Id)) {
            return false;
        }

        priceId = transaction.Items[0].Price!.Id;
        return true;
    }

    private static Result<CreateTransactionResponse?> InvalidRecoverableTransaction() =>
        Result.Failure<CreateTransactionResponse?>(Errors.Billing.ProviderOperationFailed(
            BillingProviderNames.Paddle,
            "Paddle recoverable transaction response is invalid."));

    private static string CreateCheckoutReference(BillingCheckoutSessionRequestModel request) =>
        string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"{request.UserId:N}:{request.Plan.Trim().ToLowerInvariant()}"
            : request.IdempotencyKey.Trim();

    private static bool IsAmbiguousNetworkFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested &&
        exception is HttpRequestException or TimeoutException or OperationCanceledException;

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

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long unixTimestamp)) {
            error = "Paddle webhook timestamp is invalid.";
            return false;
        }

        DateTimeOffset signedAt;
        try {
            signedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        } catch (ArgumentOutOfRangeException) {
            error = "Paddle webhook timestamp is invalid.";
            return false;
        }

        TimeSpan age = _timeProvider.GetUtcNow() - signedAt;
        var tolerance = TimeSpan.FromSeconds(_options.WebhookTimestampToleranceSeconds);
        if (age.Duration() > tolerance) {
            error = "Paddle webhook timestamp is outside the allowed tolerance.";
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
        _options.CheckoutEnabled &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ApiBaseUrl) &&
        !string.IsNullOrWhiteSpace(_options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(_options.PremiumYearlyPriceId) &&
        BillingUrlValidator.IsAbsoluteHttps(_options.CheckoutUrl);

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
        JsonElement firstItem = data.GetProperty("items")[0];
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

    private static decimal? ParseMoney(JsonElement data, string? currency) {
        JsonElement totals = GetTransactionTotals(data);
        return ParseMoneyProperty(totals, "total", currency);
    }

    private static JsonElement GetTransactionTotals(JsonElement data) {
        if (data.TryGetProperty("details", out JsonElement details) &&
            details.TryGetProperty("totals", out JsonElement detailsTotals)) {
            return detailsTotals;
        }

        return data.TryGetProperty("totals", out JsonElement directTotals) ? directTotals : default;
    }

    private static decimal? ParseMoneyProperty(JsonElement totals, string propertyName, string? currency) {
        string? rawValue = GetString(totals, propertyName);
        if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long minorUnits)) {
            return null;
        }

        int exponent = currency is "CLP" or "JPY" or "KRW" or "VND" ? 0 : 2;
        return minorUnits / (decimal)Math.Pow(10, exponent);
    }

    private static decimal? ApplyAdjustmentSign(decimal? amount, string? action) {
        if (!amount.HasValue) {
            return null;
        }

        bool isReversal = action is BillingPaymentKinds.ChargebackReverse or BillingPaymentKinds.CreditReverse;
        return isReversal ? Math.Abs(amount.Value) : -Math.Abs(amount.Value);
    }

    private static int? GetTotalQuantity(JsonElement data) {
        if (!data.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array) {
            return null;
        }

        int quantity = 0;
        foreach (JsonElement item in items.EnumerateArray()) {
            if (item.TryGetProperty("quantity", out JsonElement value) && value.TryGetInt32(out int itemQuantity)) {
                quantity += itemQuantity;
            }
        }

        return quantity > 0 ? quantity : null;
    }

    private sealed record CreateCustomerRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("custom_data")] IReadOnlyDictionary<string, string>? CustomData);

    private sealed record CreateCustomerResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("custom_data")] IReadOnlyDictionary<string, string>? CustomData = null);

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
        [property: JsonPropertyName("checkout")] TransactionCheckoutResponse? Checkout,
        [property: JsonPropertyName("status")] string? Status = null,
        [property: JsonPropertyName("custom_data")] IReadOnlyDictionary<string, string>? CustomData = null,
        [property: JsonPropertyName("items")] IReadOnlyList<TransactionItemResponse>? Items = null,
        [property: JsonPropertyName("created_at")] DateTimeOffset? CreatedAt = null);

    private sealed record TransactionItemResponse(
        [property: JsonPropertyName("price")] TransactionPriceResponse? Price,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record TransactionPriceResponse(
        [property: JsonPropertyName("id")] string? Id);

    private sealed record TransactionCheckoutResponse(
        [property: JsonPropertyName("url")] string? Url);

    private sealed record CreateCustomerPortalSessionResponse(
        [property: JsonPropertyName("urls")] CustomerPortalUrls? Urls);

    private sealed record CustomerPortalUrls(
        [property: JsonPropertyName("general")] CustomerPortalGeneralUrls? General);

    private sealed record CustomerPortalGeneralUrls(
        [property: JsonPropertyName("overview")] string? Overview);
}
