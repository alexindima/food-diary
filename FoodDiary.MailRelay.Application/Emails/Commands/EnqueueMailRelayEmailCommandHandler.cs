using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class EnqueueMailRelayEmailCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<EnqueueMailRelayEmailCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(EnqueueMailRelayEmailCommand command, CancellationToken cancellationToken) {
        Guid id = await useCases.EnqueueAsync(command.Request, cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(id);
    }
}
