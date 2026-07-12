namespace FoodDiary.Application.Billing.Common;

public interface IBillingMarketingConversionRecorder {
    Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default);
}
