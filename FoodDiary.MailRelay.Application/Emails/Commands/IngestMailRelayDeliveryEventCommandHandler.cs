using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestMailRelayDeliveryEventCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestMailRelayDeliveryEventCommand, Result<MailRelayDeliveryEventEntry>> {
    public Task<Result<MailRelayDeliveryEventEntry>> Handle(
        IngestMailRelayDeliveryEventCommand command,
        CancellationToken cancellationToken) {
        return ingestionService.IngestAsync(command.Request, cancellationToken);
    }
}
