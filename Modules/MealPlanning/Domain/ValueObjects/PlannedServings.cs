namespace FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

public sealed record PlannedServings {
    public int Value { get; }
    private PlannedServings(int value) => Value = value;

    public static PlannedServings FromCount(int value) {
        if (value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(value), "Servings must be positive.");
        }
        return new PlannedServings(value);
    }
}
