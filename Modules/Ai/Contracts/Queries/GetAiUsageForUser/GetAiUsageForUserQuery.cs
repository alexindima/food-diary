using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageForUser;

public sealed record GetAiUsageForUserQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid UserId) : IRequest<AiUsageSummary>;
