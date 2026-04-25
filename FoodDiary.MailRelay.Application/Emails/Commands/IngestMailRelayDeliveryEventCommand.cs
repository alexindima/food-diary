using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed record IngestMailRelayDeliveryEventCommand(IngestMailEventRequest Request)
    : IRequest<Result<MailRelayDeliveryEventEntry>>;
