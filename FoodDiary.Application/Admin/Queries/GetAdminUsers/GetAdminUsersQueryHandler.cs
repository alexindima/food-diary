using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IAdminUserReadService userReadService)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        int page = query.Page <= 0 ? 1 : query.Page;
        int limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;

        (IReadOnlyList<AdminUserModel> Items, int TotalItems) = await userReadService.GetPagedAsync(query.Search, page, limit, query.Status, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(TotalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>(Items, page, limit, totalPages, TotalItems);
        return Result.Success(response);
    }
}
