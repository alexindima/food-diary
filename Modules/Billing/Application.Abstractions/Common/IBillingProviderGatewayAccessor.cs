namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingProviderGatewayAccessor {
    IBillingProviderGateway GetActiveProvider();
    IBillingProviderGateway? GetProviderOrDefault(string provider);
}
