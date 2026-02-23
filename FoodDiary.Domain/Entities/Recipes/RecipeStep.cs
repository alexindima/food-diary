using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Recipes;

public sealed class RecipeStep : Entity<RecipeStepId> {
    private const int TitleMaxLength = 256;
    private const int InstructionMaxLength = 4000;
    private const int ImageUrlMaxLength = 2048;

    public RecipeId RecipeId { get; private set; }
    public int StepNumber { get; private set; }
    public string? Title { get; private set; }
    public string Instruction { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }

    private readonly List<RecipeIngredient> _ingredients = [];
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients.AsReadOnly();

    public Recipe Recipe { get; private set; } = null!;

    private RecipeStep() {
    }

    internal static RecipeStep Create(
        RecipeId recipeId,
        int stepNumber,
        string instruction,
        string? title = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null) {
        EnsureRecipeId(recipeId);

        if (stepNumber <= 0) {
            throw new ArgumentOutOfRangeException(nameof(stepNumber), "Step number must be greater than zero.");
        }

        var step = new RecipeStep {
            Id = RecipeStepId.New(),
            RecipeId = recipeId,
            StepNumber = stepNumber,
            Title = NormalizeTitle(title),
            Instruction = NormalizeInstruction(instruction, nameof(instruction)),
            ImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl)),
            ImageAssetId = imageAssetId
        };
        step.SetCreated();
        return step;
    }

    public void Update(string instruction, string? title = null, string? imageUrl = null, ImageAssetId? imageAssetId = null) {
        var normalizedInstruction = NormalizeInstruction(instruction, nameof(instruction));
        var normalizedTitle = NormalizeTitle(title);
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));

        if (string.Equals(Instruction, normalizedInstruction, StringComparison.Ordinal)
            && string.Equals(Title, normalizedTitle, StringComparison.Ordinal)
            && string.Equals(ImageUrl, normalizedImageUrl, StringComparison.Ordinal)
            && ImageAssetId == imageAssetId) {
            return;
        }

        Instruction = normalizedInstruction;
        Title = normalizedTitle;
        ImageUrl = normalizedImageUrl;
        ImageAssetId = imageAssetId;
        SetModified();
    }

    public RecipeIngredient AddProductIngredient(ProductId productId, double amount) {
        var ingredient = RecipeIngredient.CreateWithProduct(Id, productId, amount);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public RecipeIngredient AddNestedRecipeIngredient(RecipeId nestedRecipeId, double servings) {
        var ingredient = RecipeIngredient.CreateWithRecipe(Id, nestedRecipeId, servings);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public void RemoveIngredient(RecipeIngredient ingredient) {
        ArgumentNullException.ThrowIfNull(ingredient);

        if (_ingredients.Remove(ingredient)) {
            SetModified();
        }
    }

    private static string? NormalizeTitle(string? title) {
        return NormalizeOptionalText(title, TitleMaxLength, nameof(title));
    }

    private static string NormalizeInstruction(string instruction, string paramName) {
        if (string.IsNullOrWhiteSpace(instruction)) {
            throw new ArgumentException("Instruction is required", paramName);
        }

        var normalized = instruction.Trim();
        return normalized.Length > InstructionMaxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Instruction must be at most {InstructionMaxLength} characters.")
            : normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static void EnsureRecipeId(RecipeId recipeId) {
        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }
    }
}
