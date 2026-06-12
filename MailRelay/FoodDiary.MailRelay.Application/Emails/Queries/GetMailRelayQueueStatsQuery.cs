using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed record GetMailRelayQueueStatsQuery() : IRequest<Result<MailRelayQueueStats>>;
