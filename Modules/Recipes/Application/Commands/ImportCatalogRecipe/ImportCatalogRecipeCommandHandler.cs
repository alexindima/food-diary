using FluentValidation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;

public sealed class ImportCatalogRecipeCommandHandler(
    IRecipeCatalogIdReadService repository,
    IValidator<CreateRecipeCommand> validator,
    ISender sender)
    : IRequestHandler<ImportCatalogRecipeCommand, Result<CatalogRecipeImportResult>> {
    public async Task<Result<CatalogRecipeImportResult>> Handle(ImportCatalogRecipeCommand request, CancellationToken cancellationToken) {
        CatalogRecipeModel item = request.Recipe;
        if (item.Id == Guid.Empty) {
            return Report(item.Id, "invalid", ["Id must not be empty."]);
        }
        bool? isPublic = await repository.CatalogIdIsPublicAsync(item.Id, cancellationToken).ConfigureAwait(false);
        if (isPublic is false) {
            return Report(item.Id, "invalid", ["This ID cannot be imported."]);
        }
        if (isPublic is true) {
            return Report(item.Id, "skipped", []);
        }
        if (item.Steps?.Any(step => step?.Ingredients is null || step.Ingredients.Any(ingredient => ingredient is null)) is not false) {
            return Report(item.Id, "invalid", ["Steps and ingredients must not be null."]);
        }
        if (item.Steps.Any(step => step.ImageAssetId is not null || step.ImageAssetIds is { Count: > 0 })) {
            return Report(item.Id, "invalid", ["Catalog files must use image URLs, not environment-specific asset IDs."]);
        }
        var create = new CreateRecipeCommand(request.UserId, item.Name, item.Description, Comment: null, item.Category, item.ImageUrl, ImageAssetId: null,
            item.PrepTime, item.CookTime, item.Servings, Visibility: "Public", item.CalculateNutritionAutomatically, item.ManualCalories,
            item.ManualProteins, item.ManualFats, item.ManualCarbs, item.ManualFiber, item.ManualAlcohol, item.Steps) {
            Language = item.Language,
            LanguageConfirmed = item.LanguageConfirmed,
            CatalogImportId = item.Id,
        };
        FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(create, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) {
            return Report(item.Id, "invalid", validation.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}").ToArray());
        }
        if (request.Preview) {
            return Report(item.Id, "ready", []);
        }
        Result<RecipeModel> created = await sender.Send(create, cancellationToken).ConfigureAwait(false);
        return created.IsSuccess ? Report(item.Id, "imported", []) : Result.Failure<CatalogRecipeImportResult>(created.Error);
    }

    private static Result<CatalogRecipeImportResult> Report(Guid id, string status, IReadOnlyList<string> errors) =>
        Result.Success(new CatalogRecipeImportResult(id, status, errors));
}
