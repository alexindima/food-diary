namespace FoodDiary.Application.Marketing.Common;

public interface IMarketingConversionRecorder {
    Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default);
}
