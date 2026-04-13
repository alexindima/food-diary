using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.FavoriteRecipes;

public sealed class FavoriteRecipe : Entity<FavoriteRecipeId> {
    public UserId UserId { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public string? Name { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public Recipe Recipe { get; private set; } = null!;

    private FavoriteRecipe() {
    }

    public static FavoriteRecipe Create(UserId userId, RecipeId recipeId, string? name = null) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("RecipeId cannot be empty.", nameof(recipeId));
        }

        var favorite = new FavoriteRecipe {
            Id = FavoriteRecipeId.New(),
            UserId = userId,
            RecipeId = recipeId,
            Name = NormalizeOptionalText(name),
            CreatedAtUtc = DomainTime.UtcNow
        };

        favorite.SetCreated();
        return favorite;
    }

    public void UpdateName(string? name) {
        var normalized = NormalizeOptionalText(name);
        if (Name != normalized) {
            Name = normalized;
            SetModified();
        }
    }

    private static string? NormalizeOptionalText(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > DomainConstants.CommentMaxLength
            ? trimmed[..DomainConstants.CommentMaxLength]
            : trimmed;
    }
}
