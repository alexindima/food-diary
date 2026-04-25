using MediatR;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class EnqueueMailRelayEmailCommandHandler(MailRelayEmailUseCases useCases)
    : IRequestHandler<EnqueueMailRelayEmailCommand, Guid> {
    public Task<Guid> Handle(EnqueueMailRelayEmailCommand command, CancellationToken cancellationToken) {
        return useCases.EnqueueAsync(command.Request, cancellationToken);
    }
}
