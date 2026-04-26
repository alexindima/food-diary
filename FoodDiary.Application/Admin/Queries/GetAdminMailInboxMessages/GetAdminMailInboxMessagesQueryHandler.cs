using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed class GetAdminMailInboxMessagesQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessagesQuery, Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>> Handle(
        GetAdminMailInboxMessagesQuery query,
        CancellationToken cancellationToken) {
        var messages = await reader.GetMessagesAsync(query.Limit, cancellationToken);
        return Result.Success(messages);
    }
}
