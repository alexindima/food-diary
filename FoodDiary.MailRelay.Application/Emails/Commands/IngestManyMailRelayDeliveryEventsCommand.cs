using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record IngestManyMailRelayDeliveryEventsCommand(IReadOnlyList<IngestMailEventRequest> Requests)
    : IRequest<IReadOnlyList<MailRelayDeliveryEventEntry>>;
