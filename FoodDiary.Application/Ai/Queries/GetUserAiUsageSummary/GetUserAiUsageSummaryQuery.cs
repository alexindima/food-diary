using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Ai.Models;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed record GetUserAiUsageSummaryQuery(Guid UserId)
    : IQuery<Result<UserAiUsageModel>>;
