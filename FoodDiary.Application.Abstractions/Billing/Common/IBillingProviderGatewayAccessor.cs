namespace FoodDiary.Application.Billing.Common;

public interface IBillingProviderGatewayAccessor {
    IBillingProviderGateway GetActiveProvider();
    IBillingProviderGateway? GetProviderOrDefault(string provider);
}
