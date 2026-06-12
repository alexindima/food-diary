using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record IngestManyMailRelayDeliveryEventsCommand(IReadOnlyList<IngestMailEventRequest> Requests)
    : IRequest<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>>;
