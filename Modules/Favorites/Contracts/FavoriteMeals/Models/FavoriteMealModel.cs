namespace FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;

public sealed record FavoriteMealModel(
    Guid Id,
    Guid MealId,
    string? Name,
    DateTime CreatedAtUtc,
    DateTime MealDate,
    string? MealType,
    double TotalCalories,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    int ItemCount) {
    public IReadOnlyList<string> ItemImageUrls { get; init; } = [];
    public string? ImageUrl { get; init; }
    public double TotalFiber { get; init; }
    public IReadOnlyList<string> ItemNames { get; init; } = [];
}
