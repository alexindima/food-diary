using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Health;

public sealed class CheckMailInboxReadinessQueryHandler(IMailInboxReadinessChecker readinessChecker)
    : IRequestHandler<CheckMailInboxReadinessQuery, Result> {
    public async Task<Result> Handle(CheckMailInboxReadinessQuery request, CancellationToken cancellationToken) {
        await readinessChecker.CheckReadyAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
