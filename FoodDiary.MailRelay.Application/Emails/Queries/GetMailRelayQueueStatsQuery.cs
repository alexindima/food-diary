using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed record GetMailRelayQueueStatsQuery() : IRequest<MailRelayQueueStats>;
