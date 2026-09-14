using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;
using Stripe;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionSubscriptionDataOptions = Stripe.Checkout.SessionSubscriptionDataOptions;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed class StripeBillingGateway(
    IOptions<StripeOptions> options,
    IStripeClient stripeClient) : IBillingProviderGateway {
    private readonly StripeOptions _options = options.Value;

    internal StripeBillingGateway(IOptions<StripeOptions> options)
        : this(options, new StripeClient(string.IsNullOrWhiteSpace(options.Value.SecretKey)
            ? "sk_not_configured"
            : options.Value.SecretKey)) {
    }

    public string Provider => global::FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames.Stripe;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(BillingErrors.ProviderNotConfigured(Provider));
        }

        try {
            return await CreateCheckoutSessionCoreAsync(request, cancellationToken).ConfigureAwait(false);
        } catch (Exception exception) when (IsProviderRequestFailure(exception, cancellationToken)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                BillingErrors.ProviderOperationFailed(Provider, "Stripe request could not be completed."));
        }
    }

    private async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionCoreAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken) {
        string idempotencyKey = ResolveIdempotencyKey(request.IdempotencyKey);
        Result<string> customerResult = await ResolveCustomerIdAsync(request, idempotencyKey, cancellationToken).ConfigureAwait(false);
        if (customerResult.IsFailure) {
            return Result.Failure<BillingCheckoutSessionModel>(customerResult.Error);
        }

        string customerId = customerResult.Value;
        string priceId = ResolvePriceId(request.Plan);
        var sessionService = new CheckoutSessionService(stripeClient);
        CheckoutSession session = await sessionService.CreateAsync(
            CreateCheckoutOptions(request, customerId, priceId),
            new RequestOptions { IdempotencyKey = $"{idempotencyKey}:session" },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(session.Id) || !BillingUrlValidator.IsAbsoluteHttps(session.Url)) {
            return Result.Failure<BillingCheckoutSessionModel>(
                BillingErrors.ProviderOperationFailed(Provider, "Stripe checkout session identifier or URL is invalid."));
        }

        return Result.Success(new BillingCheckoutSessionModel(session.Id, session.Url, customerId, priceId, request.Plan));
    }

    private async Task<Result<string>> ResolveCustomerIdAsync(
        BillingCheckoutSessionRequestModel request,
        string idempotencyKey,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(request.ExistingCustomerId)) {
            return Result.Success(request.ExistingCustomerId);
        }

        var customerService = new CustomerService(stripeClient);
        Customer customer = await customerService.CreateAsync(
            new CustomerCreateOptions {
                Email = request.Email,
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["user_id"] = request.UserId.ToString() },
            },
            new RequestOptions { IdempotencyKey = $"{idempotencyKey}:customer" },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(customer.Id)
            ? Result.Failure<string>(BillingErrors.ProviderOperationFailed(Provider, "Stripe customer identifier is missing."))
            : Result.Success(customer.Id);
    }

    private CheckoutSessionCreateOptions CreateCheckoutOptions(
        BillingCheckoutSessionRequestModel request,
        string customerId,
        string priceId) =>
        new() {
            Mode = "subscription",
            Customer = customerId,
            SuccessUrl = _options.SuccessUrl,
            CancelUrl = _options.CancelUrl,
            LineItems = [new CheckoutSessionLineItemOptions { Price = priceId, Quantity = 1 }],
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal) {
                ["user_id"] = request.UserId.ToString(),
                ["plan"] = request.Plan,
            },
            SubscriptionData = new CheckoutSessionSubscriptionDataOptions {
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                },
            },
        };

    public async Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingPortalSessionModel>(BillingErrors.ProviderNotConfigured(Provider));
        }

        try {
            var portalSessionService = new BillingPortalSessionService(stripeClient);
            Stripe.BillingPortal.Session portalSession = await portalSessionService.CreateAsync(
                new BillingPortalSessionCreateOptions {
                    Customer = request.CustomerId,
                    ReturnUrl = _options.PortalReturnUrl,
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!BillingUrlValidator.IsAbsoluteHttps(portalSession.Url)) {
                return Result.Failure<BillingPortalSessionModel>(
                    BillingErrors.ProviderOperationFailed(Provider, "Stripe portal session URL is invalid."));
            }

            return Result.Success(new BillingPortalSessionModel(portalSession.Url));
        } catch (Exception exception) when (IsProviderRequestFailure(exception, cancellationToken)) {
            return Result.Failure<BillingPortalSessionModel>(
                BillingErrors.ProviderOperationFailed(Provider, "Stripe request could not be completed."));
        }
    }

    public async Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForWebhook()) {
            return Result.Failure<BillingWebhookEventModel?>(BillingErrors.ProviderNotConfigured(Provider));
        }

        if (string.IsNullOrWhiteSpace(payload)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload)));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(signatureHeader)));
        }

        Event stripeEvent;
        try {
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);
        } catch (Exception exception) when (exception is StripeException or JsonException or ArgumentException or FormatException or InvalidOperationException or NullReferenceException) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.WebhookValidationFailed("Stripe webhook payload or signature is invalid."));
        }

        try {
            return stripeEvent.Type switch {
                "customer.subscription.created" or
                "customer.subscription.updated" or
                "customer.subscription.deleted" => Result.Success<BillingWebhookEventModel?>(
                    await MapAuthoritativeSubscriptionEventAsync(
                        (Subscription)stripeEvent.Data.Object!,
                        stripeEvent,
                        cancellationToken).ConfigureAwait(false)),
                "checkout.session.completed" => Result.Success<BillingWebhookEventModel?>(
                    await MapCheckoutCompletedEventAsync((CheckoutSession)stripeEvent.Data.Object!, stripeEvent, cancellationToken).ConfigureAwait(false)),
                "invoice.paid" or "invoice.payment_succeeded" => Result.Success<BillingWebhookEventModel?>(
                    MapPaidInvoiceEvent((Invoice)stripeEvent.Data.Object!, stripeEvent)),
                _ => Result.Success<BillingWebhookEventModel?>(value: null),
            };
        } catch (Exception exception) when (IsProviderRequestFailure(exception, cancellationToken)) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.ProviderOperationFailed(Provider, "Stripe request could not be completed."));
        } catch (Exception exception) when (exception is InvalidCastException or InvalidOperationException or NullReferenceException) {
            return Result.Failure<BillingWebhookEventModel?>(
                BillingErrors.WebhookValidationFailed("Stripe webhook price or structure is invalid."));
        }
    }

    private async Task<BillingWebhookEventModel> MapAuthoritativeSubscriptionEventAsync(
        Subscription eventSubscription,
        Event stripeEvent,
        CancellationToken cancellationToken) {
        var subscriptionService = new SubscriptionService(stripeClient);
        Subscription currentSubscription = await subscriptionService.GetAsync(
            eventSubscription.Id,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapSubscriptionEvent(currentSubscription, stripeEvent) with { IsAuthoritativeSnapshot = true };
    }

    private BillingWebhookEventModel? MapPaidInvoiceEvent(Invoice invoice, Event stripeEvent) {
        string? subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId) || !string.Equals(invoice.Status, "paid", StringComparison.Ordinal)) {
            return null;
        }
        InvoiceLineItem? line = invoice.Lines?.Data?.FirstOrDefault(item => ResolvePlan(item.Pricing?.PriceDetails?.PriceId) is not null);
        string? priceId = line?.Pricing?.PriceDetails?.PriceId;
        string plan = ResolvePlan(priceId)
            ?? throw new InvalidOperationException("Stripe invoice has no approved Premium price.");
        if (string.IsNullOrWhiteSpace(invoice.Id) || string.IsNullOrWhiteSpace(invoice.CustomerId) ||
            string.IsNullOrWhiteSpace(invoice.Currency) || invoice.AmountPaid < 0) {
            throw new InvalidOperationException("Stripe invoice payment details are invalid.");
        }

        // Invoice lines describe the purchased period; the current subscription may already be in a later period.
        return new BillingWebhookEventModel(
            stripeEvent.Id, stripeEvent.Type, invoice.CustomerId, subscriptionId,
            ExternalPaymentMethodId: null, priceId, plan, Status: "completed",
            line?.Period?.Start, line?.Period?.End,
            CancelAtPeriodEnd: false, CanceledAtUtc: null, TrialStartUtc: null, TrialEndUtc: null,
            Amount: FromStripeMinorUnits(invoice.AmountPaid, invoice.Currency),
            Currency: invoice.Currency.ToUpperInvariant(), ProviderMetadataJson: null,
            UserId: ParseUserId(ReadMetadata(invoice.Parent?.SubscriptionDetails?.Metadata, "user_id")),
            OccurredAtUtc: invoice.StatusTransitions?.PaidAt ?? stripeEvent.Created,
            ExternalPaymentId: invoice.Id, UpdatesSubscription: false);
    }

    private static decimal FromStripeMinorUnits(long amount, string currency) => currency.ToUpperInvariant() switch {
        "BIF" or "CLP" or "DJF" or "GNF" or "JPY" or "KMF" or "KRW" or "MGA" or "PYG" or "RWF" or "VND" or "VUV" or "XAF" or "XOF" or "XPF" => amount,
        "BHD" or "JOD" or "KWD" or "OMR" or "TND" => amount / 1000m,
        _ => amount / 100m,
    };

    private async Task<BillingWebhookEventModel?> MapCheckoutCompletedEventAsync(
        CheckoutSession session,
        Event stripeEvent,
        CancellationToken cancellationToken) {
        if (!string.Equals(session.Mode, "subscription", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(session.SubscriptionId) ||
            string.IsNullOrWhiteSpace(session.CustomerId)) {
            return null;
        }

        var subscriptionService = new SubscriptionService(stripeClient);
        Subscription subscription = await subscriptionService.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return MapSubscriptionEvent(subscription, stripeEvent, session.Metadata) with { IsAuthoritativeSnapshot = true };
    }

    private BillingWebhookEventModel MapSubscriptionEvent(
        Subscription subscription,
        Event stripeEvent,
        IReadOnlyDictionary<string, string>? fallbackMetadata = null) {
        IReadOnlyDictionary<string, string>? metadata = subscription.Metadata?.Count > 0 ? subscription.Metadata : fallbackMetadata;
        SubscriptionItem? firstItem = subscription.Items.Data.FirstOrDefault();
        string? externalPriceId = firstItem?.Price?.Id;
        string plan = ResolvePlan(externalPriceId)
            ?? throw new InvalidOperationException($"Stripe subscription price '{externalPriceId}' is not an approved Premium price.");

        return new BillingWebhookEventModel(
            stripeEvent.Id,
            stripeEvent.Type,
            subscription.CustomerId,
            subscription.Id,
            ExternalPaymentMethodId: null,
            externalPriceId,
            plan,
            subscription.Status,
            firstItem?.CurrentPeriodStart,
            firstItem?.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt,
            subscription.TrialStart,
            subscription.TrialEnd,
            Amount: null,
            Currency: null,
            ProviderMetadataJson: null,
            ParseUserId(ReadMetadata(metadata, "user_id")),
            stripeEvent.Created);
    }

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

    private static bool IsProviderRequestFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested &&
        exception is StripeException or HttpRequestException or TimeoutException or OperationCanceledException;

    private bool IsConfiguredForCheckout() =>
        !string.IsNullOrWhiteSpace(_options.SecretKey) &&
        !string.IsNullOrWhiteSpace(_options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(_options.PremiumYearlyPriceId) &&
        BillingUrlValidator.IsAbsoluteHttps(_options.SuccessUrl) &&
        BillingUrlValidator.IsAbsoluteHttps(_options.CancelUrl) &&
        BillingUrlValidator.IsAbsoluteHttps(_options.PortalReturnUrl);

    private bool IsConfiguredForWebhook() =>
        !string.IsNullOrWhiteSpace(_options.SecretKey) &&
        !string.IsNullOrWhiteSpace(_options.WebhookSecret);

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

    private static string ResolveIdempotencyKey(string? idempotencyKey) =>
        string.IsNullOrWhiteSpace(idempotencyKey)
            ? Guid.NewGuid().ToString("N")
            : idempotencyKey.Trim();
}
