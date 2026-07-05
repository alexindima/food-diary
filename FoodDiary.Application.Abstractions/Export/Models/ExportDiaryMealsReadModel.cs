using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Abstractions.Export.Models;

public sealed record ExportDiaryMealsReadModel(IReadOnlyList<Meal> Meals);
