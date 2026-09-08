using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;

public sealed record GetAdminUserLoginEventsQuery(
    int Page,
    int Limit,
    Guid? UserId,
    string? Search, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, string? Provider = null, string? Device = null) : IQuery<Result<PagedResponse<AdminUserLoginEventModel>>>;
