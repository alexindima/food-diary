using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelayQueueStats;

public sealed record GetMailRelayQueueStatsQuery : IRequest<Result<MailRelayQueueStats>>;
