using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;

public sealed class RecipeComment : AggregateRoot<RecipeCommentId> {
    private const int TextMaxLength = 2000;

    public UserId UserId { get; private set; }
    public RecipeId RecipeId { get; private set; }
    public string Text { get; private set; } = string.Empty;

    private RecipeComment() {
    }

    public static RecipeComment Create(UserId userId, RecipeId recipeId, string text) {
        EnsureUserId(userId);

        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }

        string normalizedText = NormalizeText(text);

        var comment = new RecipeComment {
            Id = RecipeCommentId.New(),
            UserId = userId,
            RecipeId = recipeId,
            Text = normalizedText,
        };
        comment.SetCreated();
        return comment;
    }

    public void UpdateText(string text) {
        string normalizedText = NormalizeText(text);
        if (string.Equals(Text, normalizedText, StringComparison.Ordinal)) {
            return;
        }

        Text = normalizedText;
        SetModified();
    }

    private static string NormalizeText(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            throw new ArgumentException("Comment text is required.", nameof(text));
        }

        string normalized = text.Trim();
        return normalized.Length > TextMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(text), $"Comment text must be at most {TextMaxLength} characters.")
            : normalized;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }
}
