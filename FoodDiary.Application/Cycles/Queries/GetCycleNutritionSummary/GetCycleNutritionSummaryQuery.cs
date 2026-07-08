using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;

public record GetCycleNutritionSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo) : IQuery<Result<CycleNutritionSummaryModel?>>, IUserRequest;
