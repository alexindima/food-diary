using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed record GetMailRelayMessageDetailsQuery(Guid Id) : IRequest<MailRelayMessageDetails?>;
