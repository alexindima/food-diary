using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Billing.Common;

namespace FoodDiary.Application.Marketing.Services;

public sealed class MarketingConversionRecorder(
    IMarketingAttributionEventReadRepository marketingAttributionEventReadRepository,
    IMarketingAttributionEventWriteRepository marketingAttributionEventWriteRepository,
    TimeProvider dateTimeProvider)
    : IMarketingConversionRecorder, IBillingMarketingConversionRecorder {
    private const string PremiumStartedEventType = "premium_started";

    public async Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default) {
        if (userId == Guid.Empty) {
            return;
        }

        bool premiumStartedAlreadyRecorded = await marketingAttributionEventReadRepository.ExistsForUserAsync(
            userId,
            PremiumStartedEventType,
            cancellationToken).ConfigureAwait(false);
        if (premiumStartedAlreadyRecorded) {
            return;
        }

        MarketingAttributionEventRecord? sourceEvent = await marketingAttributionEventReadRepository.GetLatestForUserAsync(
            userId,
            cancellationToken).ConfigureAwait(false);
        if (sourceEvent is null) {
            return;
        }

        await marketingAttributionEventWriteRepository.AddAsync(
            sourceEvent with {
                EventType = PremiumStartedEventType,
                OccurredAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime,
                UserId = userId,
            },
            cancellationToken).ConfigureAwait(false);
    }
}
