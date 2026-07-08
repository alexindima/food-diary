using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed record GetUserAiUsageSummaryQuery(Guid UserId)
    : IQuery<Result<UserAiUsageModel>>;
