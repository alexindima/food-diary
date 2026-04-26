using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayQueueStatsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayQueueStatsQuery, Result<MailRelayQueueStats>> {
    public async Task<Result<MailRelayQueueStats>> Handle(GetMailRelayQueueStatsQuery query, CancellationToken cancellationToken) {
        var stats = await useCases.GetStatsAsync(cancellationToken);
        return Result<MailRelayQueueStats>.Success(stats);
    }
}
