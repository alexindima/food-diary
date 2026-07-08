using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class ConfigurableBillingProviderGatewayAccessor(
    IEnumerable<IBillingProviderGateway> billingProviders,
    IOptions<BillingOptions> billingOptions)
    : IBillingProviderGatewayAccessor {
    private readonly Dictionary<string, IBillingProviderGateway> _providers = billingProviders.ToDictionary(
        provider => provider.Provider,
        StringComparer.OrdinalIgnoreCase);

    public IBillingProviderGateway GetActiveProvider() {
        string configuredProvider = billingOptions.Value.Provider.Trim();
        if (string.IsNullOrWhiteSpace(configuredProvider) ||
            !_providers.TryGetValue(configuredProvider, out IBillingProviderGateway? billingProvider)) {
            throw new InvalidOperationException(Errors.Billing.ProviderNotConfigured(configuredProvider).Message);
        }

        return billingProvider;
    }

    public IBillingProviderGateway? GetProviderOrDefault(string provider) {
        return string.IsNullOrWhiteSpace(provider) ? null : _providers.GetValueOrDefault(provider.Trim());
    }
}
