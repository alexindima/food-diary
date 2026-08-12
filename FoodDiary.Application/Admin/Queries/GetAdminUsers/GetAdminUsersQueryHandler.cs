using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IAdminUserReadService userReadService)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        int page = query.Page <= 0 ? 1 : query.Page;
        int limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;

        (IReadOnlyList<AdminUserModel> items, int totalItems) = await userReadService.GetPagedAsync(query.Search, page, limit, query.Status, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>(items, page, limit, totalPages, totalItems);
        return Result.Success(response);
    }
}
