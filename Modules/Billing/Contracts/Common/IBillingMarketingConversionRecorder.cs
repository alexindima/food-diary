namespace FoodDiary.Modules.Billing.Contracts.Common;

public interface IBillingMarketingConversionRecorder {
    Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default);
}
