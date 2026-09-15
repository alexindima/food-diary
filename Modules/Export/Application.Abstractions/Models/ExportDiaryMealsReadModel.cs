using FoodDiary.Modules.Meals.Contracts.Models;

namespace FoodDiary.Modules.Export.Application.Abstractions.Models;

public sealed record ExportDiaryMealsReadModel(
    IReadOnlyList<MealProjectionReadModel> Meals,
    bool HasMore);
