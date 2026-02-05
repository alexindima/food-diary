using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;
using FoodDiary.Contracts.Common;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    int Page,
    int Limit,
    string? Search,
    bool IncludeDeleted)
    : IQuery<Result<PagedResponse<AdminUserResponse>>>;
