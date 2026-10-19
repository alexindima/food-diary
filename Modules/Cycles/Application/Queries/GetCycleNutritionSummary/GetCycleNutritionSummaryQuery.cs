using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;

namespace FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;

public record GetCycleNutritionSummaryQuery(
    Guid? UserId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string? TimeZoneId = null) : IQuery<Result<CycleNutritionSummaryModel?>>, IUserRequest;
