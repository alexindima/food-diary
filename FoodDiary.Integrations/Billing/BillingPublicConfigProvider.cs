using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class BillingPublicConfigProvider(
    IOptions<BillingOptions> billingOptions,
    IOptions<StripeOptions> stripeOptions,
    IOptions<PaddleOptions> paddleOptions,
    IOptions<YooKassaOptions> yooKassaOptions)
    : IBillingPublicConfigProvider {
    public BillingPublicConfigModel GetPublicConfig() {
        var provider = billingOptions.Value.Provider?.Trim() ?? string.Empty;
        var availableProviders = ResolveAvailableProviders(provider);

        return new BillingPublicConfigModel(
            provider,
            availableProviders.Contains(BillingProviderNames.Paddle, StringComparer.OrdinalIgnoreCase)
                ? NullIfEmpty(paddleOptions.Value.ClientSideToken)
                : null,
            availableProviders);
    }

    private string[] ResolveAvailableProviders(string provider) {
        var providers = new List<string>();

        if (HasValidCheckoutConfiguration(provider)) {
            AddProvider(providers, provider);
        }

        if (HasValidPaddleCheckoutConfiguration(paddleOptions.Value)) {
            AddProvider(providers, BillingProviderNames.Paddle);
        }

        if (YooKassaOptions.HasValidCheckoutConfiguration(yooKassaOptions.Value)) {
            AddProvider(providers, BillingProviderNames.YooKassa);
        }

        return providers.ToArray();
    }

    private bool HasValidCheckoutConfiguration(string provider) {
        if (string.Equals(provider, BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)) {
            return HasValidPaddleCheckoutConfiguration(paddleOptions.Value);
        }

        if (string.Equals(provider, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase)) {
            return YooKassaOptions.HasValidCheckoutConfiguration(yooKassaOptions.Value);
        }

        return HasValidStripeCheckoutConfiguration(stripeOptions.Value);
    }

    private static void AddProvider(List<string> providers, string provider) {
        if (string.IsNullOrWhiteSpace(provider) ||
            providers.Contains(provider, StringComparer.OrdinalIgnoreCase)) {
            return;
        }

        providers.Add(NormalizeProvider(provider));
    }

    private static string NormalizeProvider(string provider) {
        if (string.Equals(provider, BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.Paddle;
        }

        if (string.Equals(provider, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase)) {
            return BillingProviderNames.YooKassa;
        }

        return BillingProviderNames.Stripe;
    }

    private static bool HasValidPaddleCheckoutConfiguration(PaddleOptions options) =>
        !string.IsNullOrWhiteSpace(options.ApiKey) &&
        !string.IsNullOrWhiteSpace(options.ClientSideToken) &&
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(options.CheckoutUrl, UriKind.Absolute);

    private static bool HasValidStripeCheckoutConfiguration(StripeOptions options) =>
        !string.IsNullOrWhiteSpace(options.SecretKey) &&
        !string.IsNullOrWhiteSpace(options.WebhookSecret) &&
        !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
        !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
        Uri.IsWellFormedUriString(options.SuccessUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(options.CancelUrl, UriKind.Absolute) &&
        Uri.IsWellFormedUriString(options.PortalReturnUrl, UriKind.Absolute);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
