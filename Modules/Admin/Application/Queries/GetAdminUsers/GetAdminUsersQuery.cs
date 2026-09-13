using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    int Page,
    int Limit,
    string? Search,
    UserAccountStatusFilter Status,
    UserAdministrationFilter? Filter = null)
    : IQuery<Result<PagedResponse<AdminUserModel>>>;
