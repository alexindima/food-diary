using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;

public sealed class GetAdminUserLoginEventsQueryHandler(IAdminUserLoginReadService readService)
    : IQueryHandler<GetAdminUserLoginEventsQuery, Result<PagedResponse<AdminUserLoginEventModel>>> {
    public async Task<Result<PagedResponse<AdminUserLoginEventModel>>> Handle(
        GetAdminUserLoginEventsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetEventsAsync(
            query.Page,
            query.Limit,
            query.UserId,
            query.Search,
            cancellationToken).ConfigureAwait(false);
    }
}
