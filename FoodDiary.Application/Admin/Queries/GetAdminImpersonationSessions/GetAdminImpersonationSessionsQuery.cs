using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed record GetAdminImpersonationSessionsQuery(
    int Page,
    int Limit,
    string? Search)
    : IQuery<Result<PagedResponse<AdminImpersonationSessionReadModel>>>;
