using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations.Billing;

public sealed class BillingPublicConfigProvider(
    IOptions<BillingOptions> billingOptions,
    IOptions<PaddleOptions> paddleOptions)
    : IBillingPublicConfigProvider {
    public BillingPublicConfigModel GetPublicConfig() {
        var provider = billingOptions.Value.Provider?.Trim() ?? string.Empty;

        return new BillingPublicConfigModel(
            provider,
            string.Equals(provider, Domain.Entities.Billing.BillingProviderNames.Paddle, StringComparison.OrdinalIgnoreCase)
                ? NullIfEmpty(paddleOptions.Value.ClientSideToken)
                : null);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
