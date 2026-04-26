using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestManyMailRelayDeliveryEventsCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestManyMailRelayDeliveryEventsCommand, Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> {
    public Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> Handle(
        IngestManyMailRelayDeliveryEventsCommand command,
        CancellationToken cancellationToken) {
        return ingestionService.IngestManyAsync(command.Requests, cancellationToken);
    }
}
