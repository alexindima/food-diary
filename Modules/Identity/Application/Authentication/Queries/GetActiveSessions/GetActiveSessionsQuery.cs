using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery(Guid UserId, Guid CurrentSessionId, int Page = 1, int Limit = 50)
    : IQuery<Result<IReadOnlyList<ActiveSessionModel>>>;
