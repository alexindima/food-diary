using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayMessageDetailsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayMessageDetailsQuery, Result<MailRelayMessageDetails>> {
    public async Task<Result<MailRelayMessageDetails>> Handle(
        GetMailRelayMessageDetailsQuery query,
        CancellationToken cancellationToken) {
        MailRelayMessageDetails? message = await useCases.GetMessageDetailsAsync(query.Id, cancellationToken).ConfigureAwait(false);
        return message is null
            ? Result<MailRelayMessageDetails>.Failure(MailRelayErrors.MessageNotFound(query.Id))
            : Result<MailRelayMessageDetails>.Success(message);
    }
}
