namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record ProductLabelHttpResponse(
    string? Name, string? Brand, decimal? BaseAmount, string? BaseUnit,
    decimal? Calories, decimal? Protein, decimal? Fat, decimal? Carbs,
    decimal? Fiber, decimal? Alcohol, string? Notes);
