using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Contracts.Commands.RecordPremiumConversion;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Marketing.Application.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Marketing.Application.Commands.RecordPremiumConversion;

public sealed class RecordPremiumConversionCommandHandler(IMarketingAttributionEventReadRepository marketingAttributionEventReadRepository,
    IMarketingAttributionEventWriteRepository marketingAttributionEventWriteRepository,
    TimeProvider dateTimeProvider) : IRequestHandler<RecordPremiumConversionCommand, Unit> {
    public async Task<Unit> Handle(RecordPremiumConversionCommand request, CancellationToken cancellationToken) {
        UserId userId = DecodeConsumerUserId(request.UserId);
        if (userId == UserId.Empty) {
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

    // The narrow consumer contract retains its scalar GUID; decode ownership at this boundary.
    private static UserId DecodeConsumerUserId(Guid value) => new(value);

    private static Guid CreateStableEventId(UserId userId, string eventType) {
        Span<byte> source = stackalloc byte[16 + 32];
        userId.Value.TryWriteBytes(source);
        int written = System.Text.Encoding.UTF8.GetBytes(eventType, source[16..]);
        Span<byte> hash = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(source[..(16 + written)], hash);
        return new Guid(hash[..16]);
    }

}
