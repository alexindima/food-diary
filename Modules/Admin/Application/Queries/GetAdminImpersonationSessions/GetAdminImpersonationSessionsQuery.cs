using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed record GetAdminImpersonationSessionsQuery(
    int Page,
    int Limit,
    string? Search)
    : IQuery<Result<PagedResponse<AdminImpersonationSessionReadModel>>>;
