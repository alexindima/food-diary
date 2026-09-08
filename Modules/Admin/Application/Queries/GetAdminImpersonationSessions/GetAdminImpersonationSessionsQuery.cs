using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed record GetAdminImpersonationSessionsQuery(
    int Page,
    int Limit,
    string? Search, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? ActorId = null, Guid? TargetId = null)
    : IQuery<Result<PagedResponse<AdminImpersonationSessionReadModel>>>;
