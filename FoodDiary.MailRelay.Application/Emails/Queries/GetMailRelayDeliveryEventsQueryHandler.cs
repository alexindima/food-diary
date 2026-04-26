using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayDeliveryEventsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayDeliveryEventsQuery, Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> {
    public async Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> Handle(
        GetMailRelayDeliveryEventsQuery query,
        CancellationToken cancellationToken) {
        var events = await useCases.GetDeliveryEventsAsync(query.Email, cancellationToken);
        return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Success(events);
    }
}
