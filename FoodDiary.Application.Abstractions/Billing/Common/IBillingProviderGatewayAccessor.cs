namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingProviderGatewayAccessor {
    IBillingProviderGateway GetActiveProvider();
    IBillingProviderGateway? GetProviderOrDefault(string provider);
}
