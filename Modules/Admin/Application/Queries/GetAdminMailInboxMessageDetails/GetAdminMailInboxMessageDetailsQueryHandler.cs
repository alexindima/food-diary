using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessageDetails;

public sealed class GetAdminMailInboxMessageDetailsQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessageDetailsQuery, Result<AdminMailInboxMessageDetailsModel>> {
    public async Task<Result<AdminMailInboxMessageDetailsModel>> Handle(
        GetAdminMailInboxMessageDetailsQuery query,
        CancellationToken cancellationToken) {
        AdminMailInboxMessageDetailsModel? message = await reader.GetMessageAsync(query.Id, cancellationToken).ConfigureAwait(false);
        return message is null
            ? Result.Failure<AdminMailInboxMessageDetailsModel>(AdminMailInboxErrors.MessageNotFound(query.Id))
            : Result.Success(message);
    }
}
