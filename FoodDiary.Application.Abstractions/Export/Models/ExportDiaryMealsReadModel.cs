using FoodDiary.Application.Abstractions.Meals.Models;

namespace FoodDiary.Application.Abstractions.Export.Models;

public sealed record ExportDiaryMealsReadModel(IReadOnlyList<MealConsumptionReadModel> Meals);
