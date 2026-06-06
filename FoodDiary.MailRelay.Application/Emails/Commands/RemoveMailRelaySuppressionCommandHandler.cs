using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class RemoveMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<RemoveMailRelaySuppressionCommand, Result> {
    public async Task<Result> Handle(RemoveMailRelaySuppressionCommand request, CancellationToken cancellationToken) {
        bool removed = await useCases.RemoveSuppressionAsync(request.Email, cancellationToken).ConfigureAwait(false);
        return removed
            ? Result.Success()
            : Result.Failure(MailRelayErrors.SuppressionNotFound(request.Email));
    }
}
