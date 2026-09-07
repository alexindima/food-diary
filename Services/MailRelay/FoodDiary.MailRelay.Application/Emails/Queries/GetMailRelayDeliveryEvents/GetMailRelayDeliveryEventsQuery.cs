using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelayDeliveryEvents;

public sealed record GetMailRelayDeliveryEventsQuery(string? Email)
    : IRequest<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>>;
