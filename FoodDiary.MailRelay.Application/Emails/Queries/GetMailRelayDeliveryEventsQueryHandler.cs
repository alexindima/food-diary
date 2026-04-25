using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayDeliveryEventsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayDeliveryEventsQuery, IReadOnlyList<MailRelayDeliveryEventEntry>> {
    public Task<IReadOnlyList<MailRelayDeliveryEventEntry>> Handle(
        GetMailRelayDeliveryEventsQuery query,
        CancellationToken cancellationToken) {
        return useCases.GetDeliveryEventsAsync(query.Email, cancellationToken);
    }
}
