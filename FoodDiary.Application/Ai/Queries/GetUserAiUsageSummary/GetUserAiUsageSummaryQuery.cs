using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed record GetUserAiUsageSummaryQuery(UserId UserId)
    : IQuery<Result<UserAiUsageResponse>>;
