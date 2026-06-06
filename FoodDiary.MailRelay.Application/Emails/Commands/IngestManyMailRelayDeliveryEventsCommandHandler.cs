using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestManyMailRelayDeliveryEventsCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestManyMailRelayDeliveryEventsCommand, Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> {
    public Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> Handle(
        IngestManyMailRelayDeliveryEventsCommand request,
        CancellationToken cancellationToken) {
        return ingestionService.IngestManyAsync(request.Requests, cancellationToken);
    }
}
