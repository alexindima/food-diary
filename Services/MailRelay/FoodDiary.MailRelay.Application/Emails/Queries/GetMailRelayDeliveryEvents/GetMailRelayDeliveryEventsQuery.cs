using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelayDeliveryEvents;

public sealed record GetMailRelayDeliveryEventsQuery(string? Email, int Page = 1, int Limit = 20)
    : IRequest<Result<MailRelayPage<MailRelayDeliveryEventEntry>>>;
