using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;
using Stripe;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using CheckoutSession = Stripe.Checkout.Session;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionSubscriptionDataOptions = Stripe.Checkout.SessionSubscriptionDataOptions;

namespace FoodDiary.Integrations.Billing;

public sealed class StripeBillingGateway(
    IOptions<StripeOptions> options,
    IStripeClient stripeClient) : IBillingProviderGateway {
    private readonly StripeOptions _options = options.Value;

    internal StripeBillingGateway(IOptions<StripeOptions> options)
        : this(options, new StripeClient(string.IsNullOrWhiteSpace(options.Value.SecretKey)
            ? "sk_not_configured"
            : options.Value.SecretKey)) {
    }

    public string Provider => Domain.Entities.Billing.BillingProviderNames.Stripe;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingCheckoutSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        string? customerId = request.ExistingCustomerId;
        if (string.IsNullOrWhiteSpace(customerId)) {
            var customerService = new CustomerService(stripeClient);
            Customer customer = await customerService.CreateAsync(
                new CustomerCreateOptions {
                    Email = request.Email,
                    Metadata = new Dictionary<string, string>(StringComparer.Ordinal) {
                        ["user_id"] = request.UserId.ToString(),
                    },
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            customerId = customer.Id;
        }

        string priceId = ResolvePriceId(request.Plan);
        var sessionService = new CheckoutSessionService(stripeClient);
        CheckoutSession session = await sessionService.CreateAsync(
            new CheckoutSessionCreateOptions {
                Mode = "subscription",
                Customer = customerId,
                SuccessUrl = _options.SuccessUrl,
                CancelUrl = _options.CancelUrl,
                LineItems = [
                    new CheckoutSessionLineItemOptions {
                        Price = priceId,
                        Quantity = 1,
                    },
                ],
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
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success(new BillingCheckoutSessionModel(
            session.Id,
            session.Url ?? string.Empty,
            customerId!,
            priceId,
            request.Plan));
    }

    public async Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
        BillingPortalSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForCheckout()) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        var portalSessionService = new BillingPortalSessionService(stripeClient);
        Stripe.BillingPortal.Session portalSession = await portalSessionService.CreateAsync(
            new BillingPortalSessionCreateOptions {
                Customer = request.CustomerId,
                ReturnUrl = _options.PortalReturnUrl,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success(new BillingPortalSessionModel(portalSession.Url));
    }

    public async Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (!IsConfiguredForWebhook()) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Billing.ProviderNotConfigured(Provider));
        }

        if (string.IsNullOrWhiteSpace(payload)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload)));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(signatureHeader)));
        }

        try {
            Event stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);

            return stripeEvent.Type switch {
                "customer.subscription.created" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "customer.subscription.updated" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "customer.subscription.deleted" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "checkout.session.completed" => Result.Success(
                    await MapCheckoutCompletedEventAsync((CheckoutSession)stripeEvent.Data.Object!, stripeEvent, cancellationToken).ConfigureAwait(false)),
                _ => Result.Success<BillingWebhookEventModel?>(value: null),
            };
        } catch (Exception ex) when (ex is StripeException or InvalidCastException or InvalidOperationException or NullReferenceException) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Billing.WebhookValidationFailed(ex.Message));
        }
    }

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
        return MapSubscriptionEvent(subscription, stripeEvent, session.Metadata);
    }

    private BillingWebhookEventModel MapSubscriptionEvent(
        Subscription subscription,
        Event stripeEvent,
        IReadOnlyDictionary<string, string>? fallbackMetadata = null) {
        IReadOnlyDictionary<string, string>? metadata = subscription.Metadata?.Count > 0 ? subscription.Metadata : fallbackMetadata;
        SubscriptionItem? firstItem = subscription.Items.Data.FirstOrDefault();
        string? plan = ResolvePlan(subscription.Items.Data.FirstOrDefault()?.Price?.Id)
                   ?? ReadMetadata(metadata, "plan");

        return new BillingWebhookEventModel(
            stripeEvent.Id,
            stripeEvent.Type,
            subscription.CustomerId,
            subscription.Id,
            ExternalPaymentMethodId: null,
            firstItem?.Price?.Id,
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
            ParseUserId(ReadMetadata(metadata, "user_id")));
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

    private bool IsConfiguredForCheckout() =>
        !string.IsNullOrWhiteSpace(_options.SecretKey) &&
        !string.IsNullOrWhiteSpace(_options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(_options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(_options.SuccessUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(_options.CancelUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(_options.PortalReturnUrl, UriKind.Absolute);

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
}
