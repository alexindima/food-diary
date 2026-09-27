using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Tests;

public partial class UpdateRecipeCommandHandlerTests {
    [Fact]
    public async Task Handle_ReplacesGalleries_DeletesOnlyRemovedAssets() {
        var user = User.Create("update-gallery@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Soup", 2);
        var removed = ImageAssetId.New();
        var kept = ImageAssetId.New();
        var removedStep = ImageAssetId.New();
        var keptStep = ImageAssetId.New();
        recipe.ReplaceImages([new(removed, "https://test/old", 0), new(kept, "https://test/kept", 1)]);
        recipe.AddStep(1, "Old step").ReplaceImages([new(removedStep, "https://test/old-step", 0), new(keptStep, "https://test/kept-step", 1)]);
        var cleanup = new RecordingImageAssetCleanupService();
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(CreateRecipeRepository(recipe.Id, user.Id, recipe), cleanup,
            CreateUserRepository(user), AllowImageAssetAccessService.Instance, new AllowAllProductLookupService(), new AllowAllRecipeLookupService());
        var added = Guid.NewGuid();
        UpdateRecipeCommand command = UpdateCommand(user.Id.Value, recipe.Id.Value, steps: [CreateStep(1, "Cook") with { ImageAssetIds = [keptStep.Value] }])
            with { ImageAssetIds = [kept.Value, added], };
        Result<RecipeModel> result = await handler.Handle(command, CancellationToken.None);
        RecipeModel model = ResultAssert.Success(result);
        Assert.Equal(new[] { kept.Value, added }, model.Images.Select(image => image.ImageAssetId));
        Assert.Equal(keptStep.Value, Assert.Single(Assert.Single(model.Steps).Images).ImageAssetId);
        Assert.Equal(new[] { removed, removedStep }.OrderBy(id => id.Value), cleanup.RequestedAssetIds.OrderBy(id => id.Value));
    }

    [Fact]
    public async Task Handle_InvalidGallery_DoesNotMutateOrCleanAssets() {
        var user = User.Create("invalid-update-gallery@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Original", 2);
        var image = new RecipeImage(ImageAssetId.New(), "https://test/kept", 0);
        recipe.ReplaceImages([image]);
        var cleanup = new RecordingImageAssetCleanupService();
        IRecipeRepository repository = CreateRecipeRepository(recipe.Id, user.Id, recipe);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(repository, cleanup, CreateUserRepository(user), AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService());
        Result<RecipeModel> result = await handler.Handle(UpdateCommand(user.Id.Value, recipe.Id.Value) with { ImageAssetIds = [Guid.Empty] }, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal("Original", recipe.Name);
        Assert.Equal(image.ImageAssetId, Assert.Single(recipe.Images).ImageAssetId);
        Assert.Empty(cleanup.RequestedAssetIds);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Recipe>(), Arg.Any<CancellationToken>());
    }
}
