using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Users.Queries.GetFilteredUsersForAdministration;
using FoodDiary.Application.Abstractions.Users.Queries.GetUsersForAdministration;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(ISender userReadService)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSizeOrDefault(query.Limit);

        (IReadOnlyList<UserAdminReadModel> items, int totalItems) = query.Filter is null
            ? await userReadService.Send(new GetUsersForAdministrationQuery(Search: query.Search, Page: page, Limit: limit, Status: query.Status), cancellationToken).ConfigureAwait(false)
            : await userReadService.Send(new GetFilteredUsersForAdministrationQuery(Search: query.Search, Page: page, Limit: limit, Status: query.Status, Filter: query.Filter), cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(totalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>([.. items.Select(AdminUserMappings.ToAdminModel)], page, limit, totalPages, totalItems);
        return Result.Success(response);
    }
}
