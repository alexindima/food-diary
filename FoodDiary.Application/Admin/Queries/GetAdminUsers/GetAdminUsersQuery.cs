using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    int Page,
    int Limit,
    string? Search,
    bool IncludeDeleted)
    : IQuery<Result<PagedResponse<AdminUserModel>>>;
