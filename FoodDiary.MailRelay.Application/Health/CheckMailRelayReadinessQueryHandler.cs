using MediatR;

namespace FoodDiary.MailRelay.Application.Health;

public sealed class CheckMailRelayReadinessQueryHandler(IMailRelayReadinessChecker readinessChecker)
    : IRequestHandler<CheckMailRelayReadinessQuery, Result> {
    public async Task<Result> Handle(CheckMailRelayReadinessQuery request, CancellationToken cancellationToken) {
        await readinessChecker.CheckReadyAsync(cancellationToken);
        return Result.Success();
    }
}
