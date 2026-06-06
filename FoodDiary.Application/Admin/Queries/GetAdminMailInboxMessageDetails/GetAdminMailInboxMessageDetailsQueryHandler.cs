using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;

public sealed class GetAdminMailInboxMessageDetailsQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessageDetailsQuery, Result<AdminMailInboxMessageDetailsModel>> {
    public async Task<Result<AdminMailInboxMessageDetailsModel>> Handle(
        GetAdminMailInboxMessageDetailsQuery query,
        CancellationToken cancellationToken) {
        AdminMailInboxMessageDetailsModel? message = await reader.GetMessageAsync(query.Id, cancellationToken).ConfigureAwait(false);
        return message is null
            ? Result.Failure<AdminMailInboxMessageDetailsModel>(Errors.MailInbox.MessageNotFound(query.Id))
            : Result.Success(message);
    }
}
