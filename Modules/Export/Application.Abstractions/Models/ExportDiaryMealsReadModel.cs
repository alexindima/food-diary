using FoodDiary.Application.Abstractions.Meals.Models;

namespace FoodDiary.Modules.Export.Application.Abstractions.Models;

public sealed record ExportDiaryMealsReadModel(
    IReadOnlyList<MealProjectionReadModel> Meals,
    bool HasMore);
