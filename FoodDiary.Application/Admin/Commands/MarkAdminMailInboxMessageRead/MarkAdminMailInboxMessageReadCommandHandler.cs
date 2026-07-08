using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;

public sealed class MarkAdminMailInboxMessageReadCommandHandler(IAdminMailInboxReader reader)
    : ICommandHandler<MarkAdminMailInboxMessageReadCommand, Result> {
    public async Task<Result> Handle(MarkAdminMailInboxMessageReadCommand command, CancellationToken cancellationToken) {
        bool marked = await reader.MarkMessageReadAsync(command.Id, cancellationToken).ConfigureAwait(false);
        return marked
            ? Result.Success()
            : Result.Failure(Errors.MailInbox.MessageNotFound(command.Id));
    }
}
