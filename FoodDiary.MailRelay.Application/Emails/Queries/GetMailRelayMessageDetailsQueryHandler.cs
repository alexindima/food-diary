using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries;

public sealed class GetMailRelayMessageDetailsQueryHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<GetMailRelayMessageDetailsQuery, Result<MailRelayMessageDetails>> {
    public async Task<Result<MailRelayMessageDetails>> Handle(
        GetMailRelayMessageDetailsQuery request,
        CancellationToken cancellationToken) {
        MailRelayMessageDetails? message = await useCases.GetMessageDetailsAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return message is null
            ? Result.Failure<MailRelayMessageDetails>(MailRelayErrors.MessageNotFound(request.Id))
            : Result.Success(message);
    }
}
