using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayDeliveryEventsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayDeliveryEventsQuery, Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> {
    public async Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> Handle(
        GetMailRelayDeliveryEventsQuery request,
        CancellationToken cancellationToken) {
        IReadOnlyList<MailRelayDeliveryEventEntry> events = await useCases.GetDeliveryEventsAsync(request.Email, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Success(events);
    }
}
