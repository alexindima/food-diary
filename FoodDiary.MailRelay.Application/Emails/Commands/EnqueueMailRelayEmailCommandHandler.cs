using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class EnqueueMailRelayEmailCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<EnqueueMailRelayEmailCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(EnqueueMailRelayEmailCommand request, CancellationToken cancellationToken) {
        Guid id = await useCases.EnqueueAsync(request.Request, cancellationToken).ConfigureAwait(false);
        return Result.Success(id);
    }
}
