using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetMailRelayDeliveryEvents;

public sealed class GetMailRelayDeliveryEventsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayDeliveryEventsQuery, Result<MailRelayPage<MailRelayDeliveryEventEntry>>> {
    public async Task<Result<MailRelayPage<MailRelayDeliveryEventEntry>>> Handle(
        GetMailRelayDeliveryEventsQuery request,
        CancellationToken cancellationToken) {
        MailRelayPage<MailRelayDeliveryEventEntry> events = await useCases.GetDeliveryEventsPageAsync(request.Email, request.Page, request.Limit, cancellationToken).ConfigureAwait(false);
        return Result<MailRelayPage<MailRelayDeliveryEventEntry>>.Success(events);
    }
}
