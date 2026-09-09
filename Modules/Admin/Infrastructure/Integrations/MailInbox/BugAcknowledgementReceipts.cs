using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ExceptionServices;

namespace FoodDiary.Infrastructure.Integrations.MailInbox;

internal sealed class BugAcknowledgementReceipts(FoodDiaryDbContext context) : IBugAcknowledgementReceipts {
    public Task<bool> ContainsAsync(Guid inboxId, CancellationToken cancellationToken) =>
        context.Set<BugAcknowledgementReceipt>().AnyAsync(x => x.InboxId == inboxId, cancellationToken);

    public async Task RecordAsync(Guid inboxId, CancellationToken cancellationToken) {
        var receipt = new BugAcknowledgementReceipt { InboxId = inboxId };
        context.Set<BugAcknowledgementReceipt>().Add(receipt);
        try {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException exception) {
            context.Entry(receipt).State = EntityState.Detached;
            ExceptionDispatchInfo? unexpectedFailure = await ContainsAsync(inboxId, cancellationToken).ConfigureAwait(false)
                ? null
                : ExceptionDispatchInfo.Capture(exception);
            unexpectedFailure?.Throw();
        }
    }
}
