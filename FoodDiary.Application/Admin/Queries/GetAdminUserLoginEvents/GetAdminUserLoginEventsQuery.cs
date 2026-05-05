using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;

public sealed record GetAdminUserLoginEventsQuery(
    int Page,
    int Limit,
    Guid? UserId,
    string? Search) : IQuery<Result<PagedResponse<AdminUserLoginEventModel>>>;
