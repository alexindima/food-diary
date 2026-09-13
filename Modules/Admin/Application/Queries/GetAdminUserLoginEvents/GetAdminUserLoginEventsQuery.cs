using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginEvents;

public sealed record GetAdminUserLoginEventsQuery(
    int Page,
    int Limit,
    Guid? UserId,
    string? Search, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, string? Provider = null, string? Device = null) : IQuery<Result<PagedResponse<AdminUserLoginEventModel>>>;
