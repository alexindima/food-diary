using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetAiUsageSummary;

public sealed record GetAiUsageSummaryQuery(
    DateTime FromUtc,
    DateTime ToUtc) : IRequest<AiUsageSummary>;
