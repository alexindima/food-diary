using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed record GetMailRelayDeliveryEventsQuery(string? Email)
    : IRequest<IReadOnlyList<MailRelayDeliveryEventEntry>>;
