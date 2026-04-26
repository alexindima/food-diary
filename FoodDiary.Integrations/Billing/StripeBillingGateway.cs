using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Result;
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

public sealed class StripeBillingGateway(IOptions<StripeOptions> options) : IBillingProviderGateway {
    private readonly StripeOptions _options = options.Value;

    public string Provider => Domain.Entities.Billing.BillingProviderNames.Stripe;

    public async Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
        BillingCheckoutSessionRequestModel request,
        CancellationToken cancellationToken = default) {
        StripeConfiguration.ApiKey = _options.SecretKey;

        var customerId = request.ExistingCustomerId;
        if (string.IsNullOrWhiteSpace(customerId)) {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(
                new CustomerCreateOptions {
                    Email = request.Email,
                    Metadata = new Dictionary<string, string> {
                        ["user_id"] = request.UserId.ToString(),
                    },
                },
                cancellationToken: cancellationToken);
            customerId = customer.Id;
        }

        var priceId = ResolvePriceId(request.Plan);
        var sessionService = new CheckoutSessionService();
        var session = await sessionService.CreateAsync(
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
                Metadata = new Dictionary<string, string> {
                    ["user_id"] = request.UserId.ToString(),
                    ["plan"] = request.Plan,
                },
                SubscriptionData = new CheckoutSessionSubscriptionDataOptions {
                    Metadata = new Dictionary<string, string> {
                        ["user_id"] = request.UserId.ToString(),
                        ["plan"] = request.Plan,
                    },
                },
            },
            cancellationToken: cancellationToken);

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
        StripeConfiguration.ApiKey = _options.SecretKey;

        var portalSessionService = new BillingPortalSessionService();
        var portalSession = await portalSessionService.CreateAsync(
            new BillingPortalSessionCreateOptions {
                Customer = request.CustomerId,
                ReturnUrl = _options.PortalReturnUrl,
            },
            cancellationToken: cancellationToken);

        return Result.Success(new BillingPortalSessionModel(portalSession.Url));
    }

    public async Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(payload)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(payload)));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader)) {
            return Result.Failure<BillingWebhookEventModel?>(Errors.Validation.Required(nameof(signatureHeader)));
        }

        try {
            StripeConfiguration.ApiKey = _options.SecretKey;
            var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _options.WebhookSecret);

            return stripeEvent.Type switch {
                "customer.subscription.created" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "customer.subscription.updated" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "customer.subscription.deleted" => Result.Success<BillingWebhookEventModel?>(MapSubscriptionEvent((Subscription)stripeEvent.Data.Object!, stripeEvent)),
                "checkout.session.completed" => Result.Success<BillingWebhookEventModel?>(
                    await MapCheckoutCompletedEventAsync((CheckoutSession)stripeEvent.Data.Object!, stripeEvent, cancellationToken)),
                _ => Result.Success<BillingWebhookEventModel?>(null),
            };
        } catch (StripeException ex) {
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

        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
        return MapSubscriptionEvent(subscription, stripeEvent, session.Metadata);
    }

    private BillingWebhookEventModel MapSubscriptionEvent(
        Subscription subscription,
        Event stripeEvent,
        IReadOnlyDictionary<string, string>? fallbackMetadata = null) {
        var metadata = subscription.Metadata?.Count > 0 ? subscription.Metadata : fallbackMetadata;
        var firstItem = subscription.Items.Data.FirstOrDefault();
        var plan = ResolvePlan(subscription.Items.Data.FirstOrDefault()?.Price?.Id)
                   ?? ReadMetadata(metadata, "plan");

        return new BillingWebhookEventModel(
            stripeEvent.Id,
            stripeEvent.Type,
            subscription.CustomerId,
            subscription.Id,
            firstItem?.Price?.Id,
            plan,
            subscription.Status,
            firstItem?.CurrentPeriodStart,
            firstItem?.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt,
            subscription.TrialStart,
            subscription.TrialEnd,
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
}
