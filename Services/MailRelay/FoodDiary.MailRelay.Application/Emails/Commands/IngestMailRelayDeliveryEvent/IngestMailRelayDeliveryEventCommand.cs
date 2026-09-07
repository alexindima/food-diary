using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands.IngestMailRelayDeliveryEvent;

public sealed record IngestMailRelayDeliveryEventCommand(IngestMailEventRequest Request)
    : IRequest<Result<MailRelayDeliveryEventEntry>>;
