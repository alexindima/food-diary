using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class RemoveMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<RemoveMailRelaySuppressionCommand, bool> {
    public Task<bool> Handle(RemoveMailRelaySuppressionCommand command, CancellationToken cancellationToken) {
        return useCases.RemoveSuppressionAsync(command.Email, cancellationToken);
    }
}
