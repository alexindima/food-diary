using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class CreateMailRelaySuppressionCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<CreateMailRelaySuppressionCommand, Result> {
    public async Task<Result> Handle(CreateMailRelaySuppressionCommand request, CancellationToken cancellationToken) {
        await useCases.CreateSuppressionAsync(request.Request, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
