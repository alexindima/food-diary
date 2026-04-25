using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayMessageDetailsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayMessageDetailsQuery, MailRelayMessageDetails?> {
    public Task<MailRelayMessageDetails?> Handle(
        GetMailRelayMessageDetailsQuery query,
        CancellationToken cancellationToken) {
        return useCases.GetMessageDetailsAsync(query.Id, cancellationToken);
    }
}
