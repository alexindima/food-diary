using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Commands.DuplicateRecipe;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;

using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Tests.CentralRelocated;

public partial class RecipesFeatureTests {
    [Fact]
    public async Task Create_WhenPreferredLanguageLookupFails_DoesNotSaveRecipe() {
        var user = User.Create("profile-error@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        FoodDiary.Modules.Users.Contracts.Common.IUserProfileReadService profiles = Substitute.For<FoodDiary.Modules.Users.Contracts.Common.IUserProfileReadService>();
        var error = new Error("User.NotFound", "Profile unavailable");
        profiles.GetUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<FoodDiary.Modules.Users.Contracts.Models.UserModel>(error));
        var handler = new CreateRecipeCommandHandler(repository, repository, new StubUserRepository(user),
            AllowImageAssetAccessService.Instance, new AllowAllProductLookupService(), new AllowAllRecipeLookupService(),
            new ImmediateRecipeMutationTransactionRunner(), profiles);

        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task Create_WithAccessibleGallery_PreservesOrderAndConfirmsLanguage() {
        var user = User.Create("create-gallery@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        Guid[] assets = [Guid.NewGuid(), Guid.NewGuid()];
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(user), AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService());

        RecipeModel model = ResultAssert.Success(await handler.Handle(CreateRecipeCommand(user.Id.Value) with {
            ImageAssetIds = assets,
            Language = "ru",
            LanguageConfirmed = true,
        }, CancellationToken.None));

        Assert.Equal(assets, model.Images.Select(image => image.ImageAssetId));
        Assert.Equal("ru", model.Language);
        Assert.True(repository.LastAddedRecipe!.LanguageConfirmed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Create_WithInvalidGallery_RejectsWithoutSaving(int invalidKind) {
        var user = User.Create("gallery@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(user), AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService());
        var repeated = Guid.NewGuid();
        Guid[] ids = invalidKind switch {
            0 => [Guid.Empty],
            1 => [repeated, repeated],
            _ => [.. Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())],
        };
        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(user.Id.Value) with { ImageAssetIds = ids }, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_WithInaccessibleGallery_PropagatesErrorWithoutSaving(bool onStep) {
        var user = User.Create("gallery-denied@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        var error = new Error("Image.Forbidden", "Not owned");
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(user), new FailingNonNullImageAssetAccessService(error),
            new AllowAllProductLookupService(), new AllowAllRecipeLookupService());
        CreateRecipeCommand command = CreateRecipeCommand(user.Id.Value,
            steps: onStep ? [CreateRecipeCreateStep(1, "Cook") with { ImageAssetIds = [Guid.NewGuid()] }] : []);
        if (!onStep) { command = command with { ImageAssetIds = [Guid.NewGuid()] }; }
        Result<RecipeModel> result = await handler.Handle(command, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task Duplicate_OwnGallery_PreservesRecipeAndStepImageOrder() {
        var user = User.Create("duplicate-gallery@example.com", "hash");
        var original = Recipe.Create(user.Id, "Soup", 2);
        original.SetManualNutrition(100, 1, 2, 3, 4, 0);
        RecipeImage[] gallery = [new(ImageAssetId.New(), "https://test/second", 0), new(ImageAssetId.New(), "https://test/first", 1)];
        original.ReplaceImages(gallery);
        original.AddStep(1, "Cook").ReplaceImages(gallery.Reverse().Select(image => new RecipeImage(image.ImageAssetId, image.ImageUrl, 0)).ToArray());
        original.Steps.Single().AddTextIngredient("Salt", "to taste");
        var repository = new SingleRecipeRepository(original);
        Result<RecipeModel> result = await DuplicateRecipeHandler(repository, new StubUserRepository(user))
            .Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);
        RecipeModel model = ResultAssert.Success(result);
        Assert.Equal(gallery.Select(image => image.ImageAssetId.Value), model.Images.Select(image => image.ImageAssetId));
        Assert.Equal(gallery.Reverse().Select(image => image.ImageAssetId.Value), Assert.Single(model.Steps).Images.Select(image => image.ImageAssetId));
        Assert.Equal(model.Images[0].ImageAssetId, model.ImageAssetId);
        Assert.Equal("Salt", Assert.Single(Assert.Single(model.Steps).Ingredients).TextName);
    }
}
