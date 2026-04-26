namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaFoodPortionModel(
    int Id,
    double Amount,
    string MeasureUnitName,
    double GramWeight,
    string? PortionDescription,
    string? Modifier);
