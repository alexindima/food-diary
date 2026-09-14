using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Application.Abstractions.Models;

namespace FoodDiary.Modules.Ai.Application.Queries.GetUserAiUsageSummary;

public sealed record GetUserAiUsageSummaryQuery(Guid UserId)
    : IQuery<Result<UserAiUsageModel>>;
