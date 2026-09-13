using FoodDiary.Modules.Admin.Infrastructure.Persistence;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Admin.PersistenceModel;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ExceptionServices;

namespace FoodDiary.Modules.Admin.Infrastructure.Integrations.MailInbox;

internal sealed class BugAcknowledgementReceipts(AdminDbContext context, IUnitOfWork unitOfWork) : IBugAcknowledgementReceipts {
    public Task<bool> ContainsAsync(Guid inboxId, CancellationToken cancellationToken) =>
        context.BugAcknowledgementReceipts.AnyAsync(x => x.InboxId == inboxId, cancellationToken);

    public async Task RecordAsync(Guid inboxId, CancellationToken cancellationToken) {
        var receipt = new BugAcknowledgementReceipt { InboxId = inboxId };
        context.BugAcknowledgementReceipts.Add(receipt);
        try {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException exception) {
            context.Entry(receipt).State = EntityState.Detached;
            ExceptionDispatchInfo? unexpectedFailure = await ContainsAsync(inboxId, cancellationToken).ConfigureAwait(false)
                ? null
                : ExceptionDispatchInfo.Capture(exception);
            unexpectedFailure?.Throw();
        }
    }
}
