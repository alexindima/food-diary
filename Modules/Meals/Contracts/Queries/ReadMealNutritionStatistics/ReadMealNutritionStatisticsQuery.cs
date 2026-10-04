using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Contracts.Queries.ReadMealNutritionStatistics;

// Trusted composition read: callers own authorization for the supplied user.
public sealed record ReadMealNutritionStatisticsQuery(
    UserId UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    string? TimeZoneId = null) : IQuery<Result<IReadOnlyList<MealNutritionStatisticsBucket>>>;
