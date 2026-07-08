using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: null,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: 0,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("calories", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Theory]
    [InlineData(100.0, null, 4.0, 20.0, 2.0, 0.0, "proteins")]
    [InlineData(100.0, 10.0, null, 20.0, 2.0, 0.0, "fats")]
    [InlineData(100.0, 10.0, 4.0, null, 2.0, 0.0, "carbs")]
    [InlineData(100.0, 10.0, 4.0, 20.0, null, 0.0, "fiber")]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionRequiredValueIsMissing_ReturnsValidationFailure(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol,
        string expectedField) {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("manual-missing@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                userId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: calories,
                manualProteins: proteins,
                manualFats: fats,
                manualCarbs: carbs,
                manualFiber: fiber,
                manualAlcohol: alcohol,
                steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains(expectedField, result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionIsNegative_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("manual-negative@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                userId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: 100,
                manualProteins: 10,
                manualFats: -1,
                manualCarbs: 20,
                manualFiber: 2,
                manualAlcohol: 0,
                steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("missing-user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(userId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("bad-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(UserId.New().Value, visibility: "secret"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        RecordingImageAssetAccessService imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("image-fail@example.com", "hash")),
            imageAccess,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(UserId.New().Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WhenStepImageAssetAccessFails_ReturnsFailure() {
        var stepImageAssetId = ImageAssetId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("step-image-fail@example.com", "hash")),
            new FailingNonNullImageAssetAccessService(Errors.Image.Forbidden()),
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                UserId.New().Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Step with image",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: stepImageAssetId.Value,
                        Ingredients: []),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(User.Create("empty-nested-recipe@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                UserId.New().Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Use nested recipe",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: null,
                        Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.Empty, Amount: 1)]),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithNestedRecipeIngredient_PersistsNestedIngredient() {
        var user = User.Create("nested-create@example.com", "hash");
        var nestedRecipeId = RecipeId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                user.Id.Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Use nested recipe",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: null,
                        Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: nestedRecipeId.Value, Amount: 2)]),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        RecipeIngredient ingredient = Assert.Single(Assert.Single(repository.LastAddedRecipe!.Steps).Ingredients);
        Assert.Equal(nestedRecipeId, ingredient.NestedRecipeId);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithValidCommand_PersistsAndReturnsOwnedModel() {
        var user = User.Create("create-recipe@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Tomato Soup",
                Description: "Creamy soup",
                Comment: "Serve warm",
                Category: "Dinner",
                ImageUrl: "https://cdn.test/soup.png",
                ImageAssetId: null,
                PrepTime: 15,
                CookTime: 30,
                Servings: 4,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: 320,
                ManualProteins: 14,
                ManualFats: 9,
                ManualCarbs: 40,
                ManualFiber: 6,
                ManualAlcohol: 0,
                Steps: [
                    CreateRecipeCreateStep(order: 1, "Chop vegetables"),
                    CreateRecipeCreateStep(order: 2, "Boil soup"),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.Equal("Tomato Soup", repository.LastAddedRecipe.Name);
        Assert.Equal(2, repository.LastAddedRecipe.Steps.Count);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Serve warm", result.Value.Comment);
        Assert.Equal(2, result.Value.Steps.Count);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithInaccessibleProductIngredient_ReturnsValidationFailure() {
        var user = User.Create("create-recipe-inaccessible-product@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(
            repository,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new EmptyProductLookupService(),
            new AllowAllRecipeLookupService());

        var productId = Guid.NewGuid();
        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Tomato Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 15,
                CookTime: 30,
                Servings: 4,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeStepWithProduct(order: 1, "Chop vegetables", productId)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Product", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastAddedRecipe);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: Guid.Empty,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyStepImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: Guid.Empty,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyIngredientProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.Empty, NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task CreateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new SingleRecipeRepositoryForCreate();
        CreateRecipeCommandHandler handler = CreateRecipeHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

}
