using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class EnqueueMailRelayEmailCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<EnqueueMailRelayEmailCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(EnqueueMailRelayEmailCommand command, CancellationToken cancellationToken) {
        var id = await useCases.EnqueueAsync(command.Request, cancellationToken);
        return Result<Guid>.Success(id);
    }
}
