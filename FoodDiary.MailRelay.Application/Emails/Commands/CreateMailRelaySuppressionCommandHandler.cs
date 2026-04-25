using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class CreateMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<CreateMailRelaySuppressionCommand> {
    public Task Handle(CreateMailRelaySuppressionCommand command, CancellationToken cancellationToken) {
        return useCases.CreateSuppressionAsync(command.Request, cancellationToken);
    }
}
