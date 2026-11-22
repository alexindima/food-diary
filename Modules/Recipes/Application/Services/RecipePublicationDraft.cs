using System.Text.Json;
using System.Collections.ObjectModel;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Services;

internal sealed record RecipePublicationDraft(UpdateRecipeCommand Change, RecipeModel Preview) {
    public static RecipePublicationDraft Capture(Recipe recipe) {
        var change = new UpdateRecipeCommand(
            recipe.UserId.Value,
            recipe.Id.Value,
            recipe.Name,
            recipe.Description,
            ClearDescription: recipe.Description is null,
            Comment: null,
            ClearComment: false,
            recipe.Category.ToCode(),
            ClearCategory: false,
            recipe.ImageUrl,
            ClearImageUrl: recipe.ImageUrl is null,
            recipe.ImageAssetId?.Value,
            ClearImageAssetId: recipe.ImageAssetId is null,
            recipe.PrepTime,
            recipe.CookTime,
            recipe.Servings,
            nameof(Visibility.Public),
            recipe.IsNutritionAutoCalculated,
            recipe.ManualCalories,
            recipe.ManualProteins,
            recipe.ManualFats,
            recipe.ManualCarbs,
            recipe.ManualFiber,
            recipe.ManualAlcohol,
            recipe.Steps.OrderBy(step => step.StepNumber).Select(ToStepInput).ToArray()) {
            ClearPrepTime = recipe.PrepTime is null,
            ClearCookTime = recipe.CookTime is null,
            Language = recipe.Language,
            LanguageConfirmed = recipe.LanguageConfirmed,
            ImageAssetIds = AssetIds(recipe.Images),
        };
        RecipeModel preview = recipe.ToModel(usageCount: 0, isOwnedByCurrentUser: true) with { Comment = null };
        return new RecipePublicationDraft(change, WithIngredientIds(preview, change.Steps!));
    }

    public static RecipePublicationDraft? Read(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<RecipePublicationDraft>(json);

    public string Serialize() => JsonSerializer.Serialize(this);

    public static async Task<RecipeModel> StageNewAsync(
        Recipe recipe,
        IRecipePublicationQueue? queue,
        CancellationToken cancellationToken) {
        if (queue?.Enabled != true || recipe.Visibility != Visibility.Public) {
            return recipe.ToModel(usageCount: 0, isOwnedByCurrentUser: true);
        }

        RecipePublicationDraft draft = Capture(recipe);
        recipe.ChangeVisibility(Visibility.Private);
        RecipePublicationState state = await queue.StageAsync(
            recipe.Id, recipe.UserId, draft.Serialize(), BuildContent(recipe), cancellationToken);
        return WithPublication(recipe.ToModel(usageCount: 0, isOwnedByCurrentUser: true), state, includePreview: false);
    }

    public bool HasSamePublicContent(RecipePublicationDraft other) =>
        HasSamePublicContent(other.Change);

    public bool HasSamePublicContent(UpdateRecipeCommand other) =>
        string.Equals(JsonSerializer.Serialize(Change), JsonSerializer.Serialize(other), StringComparison.Ordinal);

    public static Recipe ForEdit(Recipe current, RecipePublicationDraft? existing) {
        RecipeModel model = existing?.Preview ?? current.ToModel(usageCount: 0, isOwnedByCurrentUser: true);
        var recipe = Recipe.Create(
            current.UserId,
            model.Name,
            model.Servings,
            model.Description,
            current.Comment,
            model.Category,
            model.ImageUrl,
            model.ImageAssetId is { } assetId ? new(assetId) : null,
            model.PrepTime,
            model.CookTime,
            current.Visibility,
            model.Language,
            current.Id);
        recipe.SetLanguageConfirmation(model.LanguageConfirmed);
        if (model.Images.Count > 0) {
            recipe.ReplaceImages(model.Images.Select((image, index) =>
                new RecipeImage(new(image.ImageAssetId), image.ImageUrl, index)).ToArray());
            recipe.UpdateMedia(imageUrl: model.ImageUrl, clearImageUrl: model.ImageUrl is null,
                imageAssetId: model.ImageAssetId is { } coverId ? new(coverId) : null, clearImageAssetId: model.ImageAssetId is null);
        }
        return recipe;
    }

    public static RecipeModel WithPublication(RecipeModel current, RecipePublicationState? state, bool includePreview = true) {
        if (state is null || !current.IsOwnedByCurrentUser) {
            return current;
        }

        RecipeModel presentation = includePreview && Read(state.ChangeJson) is { } draft ? draft.Preview : current;
        return presentation with {
            Visibility = current.Visibility,
            Comment = current.Comment,
            CreatedAt = current.CreatedAt,
            UsageCount = current.UsageCount,
            IsFavorite = current.IsFavorite,
            FavoriteRecipeId = current.FavoriteRecipeId,
            PublicationRevisionId = state.RevisionId,
            PublicationStatus = state.Status,
            PublicationReason = state.Reason,
        };
    }

    public static async Task<IReadOnlyDictionary<RecipeId, RecipePublicationState>> GetOwnedStatesAsync(
        IEnumerable<RecipeModel> models,
        IRecipePublicationQueue? queue,
        CancellationToken cancellationToken) {
        RecipeId[] ids = [.. models.Where(model => model.IsOwnedByCurrentUser).Select(model => new RecipeId(model.Id)).Distinct()];
        return queue is null || ids.Length == 0
            ? ReadOnlyDictionary<RecipeId, RecipePublicationState>.Empty
            : await queue.GetCurrentAsync(ids, cancellationToken);
    }

    public static RecipeModel WithPublication(RecipeModel model, IReadOnlyDictionary<RecipeId, RecipePublicationState> states) =>
        WithPublication(model, states.GetValueOrDefault(new RecipeId(model.Id)), includePreview: false);

    public static RecipeModerationContent BuildContent(Recipe recipe) {
        var text = new List<string> { recipe.Name };
        AddText(text, recipe.Description);

        var images = new List<RecipeModerationImage>();
        AddPhotos(images, "recipe", recipe.ImageAssetId?.Value, recipe.ImageUrl, recipe.Images);
        foreach (RecipeStep step in recipe.Steps.OrderBy(step => step.StepNumber)) {
            AddText(text, step.Title);
            AddText(text, step.Instruction);
            foreach (RecipeIngredient ingredient in step.Ingredients) {
                AddText(text, ingredient.TextName);
                AddText(text, ingredient.AmountText);
                AddText(text, ingredient.PublicName);
                AddText(text, ingredient.PublicUnit);
            }
            AddPhotos(images, FormattableString.Invariant($"step/{step.StepNumber}"), step.ImageAssetId?.Value, step.ImageUrl, step.Images);
        }

        return new RecipeModerationContent(string.Join('\n', text), images, recipe.UserId.Value);
    }

    private static RecipeStepInput ToStepInput(RecipeStep step) =>
        new(step.StepNumber, step.Instruction, step.Title, step.ImageUrl, step.ImageAssetId?.Value,
            step.Ingredients.Select(ingredient =>
                new RecipeIngredientInput(ingredient.ProductId?.Value, ingredient.NestedRecipeId?.Value, ingredient.Amount) {
                    PublicName = ingredient.PublicName,
                    PublicUnit = ingredient.PublicUnit,
                    TextName = ingredient.TextName,
                    AmountText = ingredient.AmountText,
                }).ToArray()) { ImageAssetIds = AssetIds(step.Images) };

    private static Guid[]? AssetIds(IReadOnlyCollection<RecipeImage> images) =>
        images.Count == 0 ? null : [.. images.OrderBy(image => image.Position).Select(image => image.ImageAssetId.Value)];

    private static RecipeModel WithIngredientIds(RecipeModel model, IReadOnlyList<RecipeStepInput> steps) =>
        model with {
            Steps = model.Steps.Select((step, stepIndex) => step with {
                Ingredients = step.Ingredients.Select((ingredient, ingredientIndex) => {
                    RecipeIngredientInput input = steps[stepIndex].Ingredients[ingredientIndex];
                    return ingredient with { ProductId = input.ProductId, NestedRecipeId = input.NestedRecipeId };
                }).ToArray(),
            }).ToArray(),
        };

    private static void AddText(List<string> text, string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            text.Add(value);
        }
    }

    private static void AddPhotos(
        List<RecipeModerationImage> result,
        string field,
        Guid? assetId,
        string? url,
        IReadOnlyCollection<RecipeImage> gallery) {
        if ((assetId.HasValue || !string.IsNullOrWhiteSpace(url)) && !gallery.Any(image =>
            image.ImageAssetId.Value == assetId || string.Equals(image.ImageUrl, url, StringComparison.Ordinal))) {
            result.Add(new RecipeModerationImage($"{field}/cover", assetId, url ?? string.Empty));
        }
        result.AddRange(gallery.OrderBy(image => image.Position).Select(image =>
            new RecipeModerationImage(FormattableString.Invariant($"{field}/photo/{image.Position + 1}"), image.ImageAssetId.Value, image.ImageUrl)));
    }
}
