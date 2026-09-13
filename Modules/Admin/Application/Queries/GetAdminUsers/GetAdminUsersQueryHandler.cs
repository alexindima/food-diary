using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IUserAdministrationReadService userReadService)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSizeOrDefault(query.Limit);

        (IReadOnlyList<UserAdminReadModel> items, int totalItems) = query.Filter is null
            ? await userReadService.GetPagedAsync(query.Search, page, limit, query.Status, cancellationToken).ConfigureAwait(false)
            : await userReadService.GetFilteredPagedAsync(query.Search, page, limit, query.Status, query.Filter, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>([.. items.Select(AdminUserMappings.ToAdminModel)], page, limit, totalPages, totalItems);
        return Result.Success(response);
    }
}
