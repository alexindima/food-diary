using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;

namespace FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;

public record GetCycleNutritionSummaryQuery(
    Guid? UserId,
    DateOnly DateFrom,
    DateOnly DateTo) : IQuery<Result<CycleNutritionSummaryModel?>>, IUserRequest;
