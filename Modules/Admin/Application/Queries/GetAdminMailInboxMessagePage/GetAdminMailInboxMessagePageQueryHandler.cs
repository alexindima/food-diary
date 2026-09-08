using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;

public sealed class GetAdminMailInboxMessagePageQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessagePageQuery, Result<AdminMailInboxMessagePageModel>> {
    public async Task<Result<AdminMailInboxMessagePageModel>> Handle(GetAdminMailInboxMessagePageQuery query, CancellationToken cancellationToken) {
        AdminMailInboxMessagePageModel page = await reader.GetMessagePageAsync(query.Page, query.Limit, query.Recipient, query.Category, query.Unread, cancellationToken).ConfigureAwait(false);
        return Result.Success(page);
    }
}

