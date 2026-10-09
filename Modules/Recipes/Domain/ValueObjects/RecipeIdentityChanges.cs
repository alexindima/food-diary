using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Recipes.Domain.ValueObjects;

public sealed record RecipeIdentityChanges(
    string? Name,
    FieldChange<string> Description,
    FieldChange<string> Comment,
    FieldChange<string> Category);
