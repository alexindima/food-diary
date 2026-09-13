using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessagePage;

public sealed class GetAdminMailInboxMessagePageQueryHandler(IAdminMailInboxReader reader)
    : IQueryHandler<GetAdminMailInboxMessagePageQuery, Result<AdminMailInboxMessagePageModel>> {
    public async Task<Result<AdminMailInboxMessagePageModel>> Handle(GetAdminMailInboxMessagePageQuery query, CancellationToken cancellationToken) {
        AdminMailInboxMessagePageModel page = await reader.GetMessagePageAsync(query.Page, query.Limit, query.Recipient, query.Category, query.Unread, cancellationToken, query.FromUtc, query.ToUtc, query.Search, query.FromAddress, query.Id).ConfigureAwait(false);
        return Result.Success(page);
    }
}

