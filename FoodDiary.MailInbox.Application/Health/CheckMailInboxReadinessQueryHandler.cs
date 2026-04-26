using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Health;

public sealed class CheckMailInboxReadinessQueryHandler(IMailInboxReadinessChecker readinessChecker)
    : IRequestHandler<CheckMailInboxReadinessQuery, Result> {
    public async Task<Result> Handle(CheckMailInboxReadinessQuery request, CancellationToken cancellationToken) {
        await readinessChecker.CheckReadyAsync(cancellationToken);
        return Result.Success();
    }
}
