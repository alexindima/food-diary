using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipeStep : Entity<RecipeStepId> {
    private readonly List<RecipeImage> _images = [];
    public IReadOnlyCollection<RecipeImage> Images => _images.AsReadOnly();
    public void ReplaceImages(IReadOnlyList<RecipeImage> images) {
        if (images.Count > 5 || images.Select(image => image.ImageAssetId).Distinct().Count() != images.Count) {
            throw new ArgumentException("A recipe can have up to five distinct images.", nameof(images));
        }
        _images.RemoveAll(image => !images.Any(next => next.ImageAssetId == image.ImageAssetId));
        for (int index = 0; index < images.Count; index++) {
            RecipeImage? existing = _images.Find(image => image.ImageAssetId == images[index].ImageAssetId);
            if (existing is null) { _images.Add(new RecipeImage(images[index].ImageAssetId, images[index].ImageUrl, index)); } else { existing.SetPosition(index); }
        }
        ImageAssetId = images.Count > 0 ? images[0].ImageAssetId : null;
        ImageUrl = images.Count > 0 ? images[0].ImageUrl : null;
        SetModified();
    }

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

        var content = RecipeStepContentState.Create(instruction, title, imageUrl, imageAssetId);
        var step = new RecipeStep {
            Id = RecipeStepId.New(),
            RecipeId = recipeId,
            StepNumber = stepNumber,
        };
        step.ApplyContentState(content);
        step.SetCreated();
        return step;
    }

    public void Update(string instruction, string? title = null, string? imageUrl = null, ImageAssetId? imageAssetId = null) {
        var content = RecipeStepContentState.Create(instruction, title, imageUrl, imageAssetId);

        if (GetContentState() == content) {
            return;
        }

        ApplyContentState(content);
        SetModified();
    }

    public RecipeIngredient AddTextIngredient(string name, string? amountText = null) {
        var ingredient = RecipeIngredient.CreateWithText(Id, name, amountText);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public RecipeIngredient AddProductIngredient(ProductId productId, double amount) {
        var ingredient = RecipeIngredient.CreateWithProduct(Id, productId, amount);
        _ingredients.Add(ingredient);
        SetModified();
        return ingredient;
    }

    public RecipeIngredient AddNestedRecipeIngredient(RecipeId nestedRecipeId, double servings) {
        if (nestedRecipeId == RecipeId) {
            throw new ArgumentException("A recipe cannot contain itself as an ingredient.", nameof(nestedRecipeId));
        }

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

    private RecipeStepContentState GetContentState() {
        return RecipeStepContentState.Create(
            Instruction,
            Title,
            ImageUrl,
            ImageAssetId);
    }

    private void ApplyContentState(RecipeStepContentState state) {
        Title = state.Title;
        Instruction = state.Instruction;
        ImageUrl = state.ImageUrl;
        ImageAssetId = state.ImageAssetId;
    }

    private static void EnsureRecipeId(RecipeId recipeId) {
        if (recipeId == RecipeId.Empty) {
            throw new ArgumentException("RecipeId is required.", nameof(recipeId));
        }
    }
}
