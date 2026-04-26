using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class RemoveMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<RemoveMailRelaySuppressionCommand, Result> {
    public async Task<Result> Handle(RemoveMailRelaySuppressionCommand command, CancellationToken cancellationToken) {
        var removed = await useCases.RemoveSuppressionAsync(command.Email, cancellationToken);
        return removed
            ? Result.Success()
            : Result.Failure(MailRelayErrors.SuppressionNotFound(command.Email));
    }
}
