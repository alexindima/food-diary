namespace FoodDiary.Modules.Products.Application.Models;

public sealed record PublicProductModel(Guid Id, string Name, string? Brand, string? ImageUrl,
    string BaseUnit, double BaseAmount, double Calories, double Proteins, double Fats, double Carbs,
    double Fiber, double Alcohol) {
    public string? Description { get; init; }
    public IReadOnlyList<string> Images { get; init; } = [];
}
