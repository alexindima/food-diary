using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Marketing.Commands.RecordPremiumConversion;

public sealed class RecordPremiumConversionCommandHandler(IMarketingAttributionEventReadRepository marketingAttributionEventReadRepository,
    IMarketingAttributionEventWriteRepository marketingAttributionEventWriteRepository,
    TimeProvider dateTimeProvider) : IRequestHandler<RecordPremiumConversionCommand, Unit> {
    public async Task<Unit> Handle(RecordPremiumConversionCommand request, CancellationToken cancellationToken) {
        Guid userId = request.UserId;
        if (userId == Guid.Empty) {
            return Unit.Value;
        }

        bool premiumStartedAlreadyRecorded = await marketingAttributionEventReadRepository.ExistsForUserAsync(
            userId,
            MarketingAttributionEventTypes.PremiumStarted,
            cancellationToken).ConfigureAwait(false);
        if (premiumStartedAlreadyRecorded) {
            return Unit.Value;
        }

        MarketingAttributionEventRecord? sourceEvent = await marketingAttributionEventReadRepository.GetLatestForUserAsync(
            userId,
            cancellationToken).ConfigureAwait(false);
        if (sourceEvent is null) {
            return Unit.Value;
        }

        await marketingAttributionEventWriteRepository.AddAsync(
            sourceEvent with {
                EventType = MarketingAttributionEventTypes.PremiumStarted,
                OccurredAtUtc = dateTimeProvider.GetUtcNow().UtcDateTime,
                UserId = userId,
                EventId = CreateStableEventId(userId, MarketingAttributionEventTypes.PremiumStarted),
            },
            cancellationToken).ConfigureAwait(false);

        return Unit.Value;
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
