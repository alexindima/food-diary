using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery(Guid UserId, Guid CurrentSessionId)
    : IQuery<Result<IReadOnlyList<ActiveSessionModel>>>;
