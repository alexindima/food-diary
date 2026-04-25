using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestMailRelayDeliveryEventCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestMailRelayDeliveryEventCommand, MailRelayDeliveryEventEntry> {
    public Task<MailRelayDeliveryEventEntry> Handle(
        IngestMailRelayDeliveryEventCommand command,
        CancellationToken cancellationToken) {
        return ingestionService.IngestAsync(command.Request, cancellationToken);
    }
}
