using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestManyMailRelayDeliveryEventsCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestManyMailRelayDeliveryEventsCommand, IReadOnlyList<MailRelayDeliveryEventEntry>> {
    public Task<IReadOnlyList<MailRelayDeliveryEventEntry>> Handle(
        IngestManyMailRelayDeliveryEventsCommand command,
        CancellationToken cancellationToken) {
        return ingestionService.IngestManyAsync(command.Requests, cancellationToken);
    }
}
