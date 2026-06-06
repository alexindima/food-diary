using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminUsers;

public sealed record GetAdminUsersQuery(
    int Page,
    int Limit,
    string? Search,
    UserAccountStatusFilter Status)
    : IQuery<Result<PagedResponse<AdminUserModel>>>;
