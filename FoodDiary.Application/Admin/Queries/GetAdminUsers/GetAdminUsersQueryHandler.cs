using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        var page = query.Page <= 0 ? 1 : query.Page;
        var limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;

        var pageData = await userRepository.GetPagedAsync(query.Search, page, limit, query.IncludeDeleted, cancellationToken);
        var users = pageData.Items.Select(user => user.ToAdminModel()).ToList();
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>(users, page, limit, totalPages, pageData.TotalItems);
        return Result.Success(response);
    }
}
