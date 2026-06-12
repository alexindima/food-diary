using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayQueueStatsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayQueueStatsQuery, Result<MailRelayQueueStats>> {
    public async Task<Result<MailRelayQueueStats>> Handle(GetMailRelayQueueStatsQuery request, CancellationToken cancellationToken) {
        MailRelayQueueStats stats = await useCases.GetStatsAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(stats);
    }
}
