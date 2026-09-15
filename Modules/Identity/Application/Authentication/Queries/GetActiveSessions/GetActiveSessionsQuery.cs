using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery(Guid UserId, Guid CurrentSessionId)
    : IQuery<Result<IReadOnlyList<ActiveSessionModel>>>;
