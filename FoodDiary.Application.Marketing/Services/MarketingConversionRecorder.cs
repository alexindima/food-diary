using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Application.Abstractions.Billing.Common;

namespace FoodDiary.Application.Marketing.Services;

public sealed class MarketingConversionRecorder(
    IMarketingAttributionEventReadRepository marketingAttributionEventReadRepository,
    IMarketingAttributionEventWriteRepository marketingAttributionEventWriteRepository,
    TimeProvider dateTimeProvider)
    : IMarketingConversionRecorder, IBillingMarketingConversionRecorder {
    public async Task RecordPremiumStartedAsync(Guid userId, CancellationToken cancellationToken = default) {
        if (userId == Guid.Empty) {
            return;
        }

        bool premiumStartedAlreadyRecorded = await marketingAttributionEventReadRepository.ExistsForUserAsync(
            userId,
            MarketingAttributionEventTypes.PremiumStarted,
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
                EventType = MarketingAttributionEventTypes.PremiumStarted,
                OccurredAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime,
                UserId = userId,
                EventId = CreateStableEventId(userId, MarketingAttributionEventTypes.PremiumStarted),
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static Guid CreateStableEventId(Guid userId, string eventType) {
        Span<byte> source = stackalloc byte[16 + 32];
        userId.TryWriteBytes(source);
        int written = System.Text.Encoding.UTF8.GetBytes(eventType, source[16..]);
        Span<byte> hash = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(source[..(16 + written)], hash);
        return new Guid(hash[..16]);
    }
}
