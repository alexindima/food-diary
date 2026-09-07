using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelayMessageDetails;

public sealed record GetMailRelayMessageDetailsQuery(Guid Id) : IRequest<Result<MailRelayMessageDetails>>;
