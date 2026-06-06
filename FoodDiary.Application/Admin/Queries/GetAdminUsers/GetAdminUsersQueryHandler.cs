using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetAdminUsersQuery, Result<PagedResponse<AdminUserModel>>> {
    public async Task<Result<PagedResponse<AdminUserModel>>> Handle(
        GetAdminUsersQuery query,
        CancellationToken cancellationToken) {
        int page = query.Page <= 0 ? 1 : query.Page;
        int limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;

        (IReadOnlyList<User> Items, int TotalItems) = await userRepository.GetPagedAsync(query.Search, page, limit, query.Status, cancellationToken).ConfigureAwait(false);
        var users = Items.Select(user => user.ToAdminModel()).ToList();
        int totalPages = (int)Math.Ceiling(TotalItems / (double)limit);
        var response = new PagedResponse<AdminUserModel>(users, page, limit, totalPages, TotalItems);
        return Result.Success(response);
    }
}
