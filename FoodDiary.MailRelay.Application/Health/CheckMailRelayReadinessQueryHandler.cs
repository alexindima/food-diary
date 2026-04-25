using MediatR;

namespace FoodDiary.MailRelay.Application.Health;

public sealed class CheckMailRelayReadinessQueryHandler(IMailRelayReadinessChecker readinessChecker)
    : IRequestHandler<CheckMailRelayReadinessQuery> {
    public Task Handle(CheckMailRelayReadinessQuery request, CancellationToken cancellationToken) {
        return readinessChecker.CheckReadyAsync(cancellationToken);
    }
}
