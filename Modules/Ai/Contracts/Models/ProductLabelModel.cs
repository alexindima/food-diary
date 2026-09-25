namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record ProductLabelModel(
    string? Name, string? Brand, decimal? BaseAmount, string? BaseUnit,
    decimal? Calories, decimal? Protein, decimal? Fat, decimal? Carbs,
    decimal? Fiber, decimal? Alcohol, string? Notes);
