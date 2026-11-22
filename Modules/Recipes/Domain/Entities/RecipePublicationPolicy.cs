namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipePublicationPolicy {
    public Guid Id { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string RulesJson { get; set; } = string.Empty;
}
