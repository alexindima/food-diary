using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed class GetAdminMailInboxMessagesQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessagesQuery, Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>> {
    public async Task<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>> Handle(
        GetAdminMailInboxMessagesQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<AdminMailInboxMessageSummaryModel> messages = query.Recipient is null && query.Category is null && query.Unread is null
            ? await reader.GetMessagesAsync(query.Limit, cancellationToken).ConfigureAwait(false)
            : await reader.GetFilteredMessagesAsync(query.Limit, query.Recipient, query.Category, query.Unread, cancellationToken).ConfigureAwait(false);
        return Result.Success(messages);
    }
}
