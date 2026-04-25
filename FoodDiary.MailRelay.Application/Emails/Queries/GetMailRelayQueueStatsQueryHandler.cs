using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayQueueStatsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayQueueStatsQuery, MailRelayQueueStats> {
    public Task<MailRelayQueueStats> Handle(GetMailRelayQueueStatsQuery query, CancellationToken cancellationToken) {
        return useCases.GetStatsAsync(cancellationToken);
    }
}
