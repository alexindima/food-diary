using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestMailRelayDeliveryEventCommandHandler(MailRelayDeliveryEventIngestionService ingestionService)
    : IRequestHandler<IngestMailRelayDeliveryEventCommand, Result<MailRelayDeliveryEventEntry>> {
    public Task<Result<MailRelayDeliveryEventEntry>> Handle(
        IngestMailRelayDeliveryEventCommand request,
        CancellationToken cancellationToken) {
        return ingestionService.IngestAsync(request.Request, cancellationToken);
    }
}
