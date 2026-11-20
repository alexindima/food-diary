namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipeModerationBudget {
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset WindowStart { get; set; }
    public int WindowSeconds { get; set; }
    public int Requests { get; set; }
}
