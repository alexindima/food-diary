using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Billing;

public sealed class ConfigurableBillingProviderGatewayAccessor(
    IEnumerable<IBillingProviderGateway> billingProviders,
    IOptions<BillingOptions> billingOptions)
    : IBillingProviderGatewayAccessor {
    private readonly Dictionary<string, IBillingProviderGateway> _providers = billingProviders.ToDictionary(
        provider => provider.Provider,
        StringComparer.OrdinalIgnoreCase);

    public IBillingProviderGateway GetActiveProvider() {
        var configuredProvider = billingOptions.Value.Provider?.Trim();
        if (string.IsNullOrWhiteSpace(configuredProvider) ||
            !_providers.TryGetValue(configuredProvider, out var billingProvider)) {
            throw new InvalidOperationException(Errors.Billing.ProviderNotConfigured(configuredProvider ?? string.Empty).Message);
        }

        return billingProvider;
    }

    public IBillingProviderGateway? GetProviderOrDefault(string provider) {
        if (string.IsNullOrWhiteSpace(provider)) {
            return null;
        }

        return _providers.GetValueOrDefault(provider.Trim());
    }
}
