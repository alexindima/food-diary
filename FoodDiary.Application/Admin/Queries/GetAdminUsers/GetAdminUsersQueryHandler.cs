using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;
using FoodDiary.Contracts.Common;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserResponse>>>
{
    public async Task<Result<PagedResponse<AdminUserResponse>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;

        var pageData = await userRepository.GetPagedAsync(query.Search, page, limit, query.IncludeDeleted);
        var users = pageData.Items.Select(user => user.ToAdminResponse()).ToList();
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);
        var response = new PagedResponse<AdminUserResponse>(users, page, limit, totalPages, pageData.TotalItems);
        return Result.Success(response);
    }
}
