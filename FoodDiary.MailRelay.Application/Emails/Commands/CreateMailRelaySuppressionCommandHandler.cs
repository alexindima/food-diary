using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class CreateMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<CreateMailRelaySuppressionCommand, Result> {
    public async Task<Result> Handle(CreateMailRelaySuppressionCommand command, CancellationToken cancellationToken) {
        await useCases.CreateSuppressionAsync(command.Request, cancellationToken);
        return Result.Success();
    }
}
