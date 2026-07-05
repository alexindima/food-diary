using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Consumptions.Models;
using FluentValidation.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Consumptions;

[ExcludeFromCodeCoverage]
public class ConsumptionsFeatureTests {
    private static UpdateConsumptionCommandHandler UpdateConsumptionHandler(
        IMealRepository repository,
        IMealNutritionService mealNutritionService,
        IRecentItemRepository recentItemRepository,
        IImageAssetCleanupService imageAssetCleanupService,
        ICurrentUserAccessService currentUserAccessService,
        TimeProvider dateTimeProvider,
        IImageAssetAccessService imageAssetAccessService) =>
        new(
            repository,
            repository,
            mealNutritionService,
            recentItemRepository,
            imageAssetCleanupService,
            currentUserAccessService,
            dateTimeProvider,
            imageAssetAccessService);

    private static DeleteConsumptionCommandHandler DeleteConsumptionHandler(IMealRepository repository) =>
        new(repository, repository);

    private static RepeatMealCommandHandler RepeatMealHandler(
        IMealRepository repository,
        IMealNutritionService mealNutritionService,
        ICurrentUserAccessService currentUserAccessService) =>
        new(repository, repository, mealNutritionService, currentUserAccessService);

    [Fact]
    public void ConsumptionItemValidator_WhenIdsAreMissing_Fails() {
        Result result = ConsumptionItemValidator.Validate(new ConsumptionItemInput(ProductId: null, RecipeId: null, 100));

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ManualNutritionValidator_WhenAlcoholIsNull_DefaultsToZero() {
        Result<ManualNutritionInput> result = ManualNutritionValidator.Validate(100, 10, 5, 20, 3, alcohol: null);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.Alcohol);
    }

    [Fact]
    public void SatietyLevelValidator_WhenPreMealOutOfRange_UsesContractFieldName() {
        Result result = SatietyLevelValidator.Validate(-1, 5);

        ResultAssert.Failure(result);
        Assert.Contains("PreMealSatietyLevel", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MealNutritionCalculator_WhenMealHasProductRecipeAndAiItems_CalculatesCombinedTotals() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);

        var product = Product.Create(
            userId,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.SetManualNutrition(200, 10, 4, 20, 2, 0);

        meal.AddProduct(product.Id, 50);
        meal.AddRecipe(recipe.Id, 1);
        meal.AddAiSession(
            imageAssetId: null,
            source: AiRecognitionSource.Text,
            recognizedAtUtc: DateTime.UtcNow,
            notes: null,
            items: [
                MealAiItemData.Create("Banana", nameLocal: null, 100, "g", 89, 1.1, 0.3, 23, 2.6, 0),
            ]);

        MealNutritionSummary result = MealNutritionCalculator.Calculate(
            meal,
            new Dictionary<ProductId, Product> { [product.Id] = product },
            new Dictionary<RecipeId, Recipe> { [recipe.Id] = recipe });

        Assert.Equal(215, result.Calories, 2);
        Assert.Equal(6.25, result.Proteins, 2);
        Assert.Equal(2.4, result.Fats, 2);
        Assert.Equal(40, result.Carbs, 2);
        Assert.Equal(4.8, result.Fiber, 2);
        Assert.Equal(0, result.Alcohol, 2);
    }

    [Fact]
    public void MealNutritionCalculator_WhenRecipeItemIsMissingFromLookup_IgnoresRecipeItem() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);

        meal.AddRecipe(RecipeId.New(), 1);

        MealNutritionSummary result = MealNutritionCalculator.Calculate(
            meal,
            new Dictionary<ProductId, Product>(),
            new Dictionary<RecipeId, Recipe>());

        Assert.Equal(0, result.Calories);
        Assert.Equal(0, result.Proteins);
        Assert.Equal(0, result.Fats);
        Assert.Equal(0, result.Carbs);
        Assert.Equal(0, result.Fiber);
        Assert.Equal(0, result.Alcohol);
    }

    [Fact]
    public void ConsumptionHttpMappings_CreateToCommand_WhenListsAreNull_MapsEmptyCollections() {
        var request = new CreateConsumptionHttpRequest(
            DateTime.UtcNow,
            MealType.Breakfast.ToString(),
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: null!,
            AiSessions: null);

        CreateConsumptionCommand command = request.ToCommand(Guid.NewGuid());

        Assert.Empty(command.Items);
        Assert.Empty(command.AiSessions);
    }

    [Fact]
    public void ConsumptionHttpMappings_UpdateToCommand_WhenAiItemsAreNull_MapsEmptyCollection() {
        var request = new UpdateConsumptionHttpRequest(
            DateTime.UtcNow,
            MealType.Dinner.ToString(),
            Comment: "ok",
            ImageUrl: null,
            ImageAssetId: null,
            Items: [
                new ConsumptionItemHttpRequest(ProductId.New().Value, RecipeId: null, 150),
            ],
            AiSessions: [
                new ConsumptionAiSessionHttpRequest(
                    ImageAssetId: null,
                    Source: "Text",
                    RecognizedAtUtc: DateTime.UtcNow,
                    Notes: null,
                    Items: null!),
            ]);

        UpdateConsumptionCommand command = request.ToCommand(Guid.NewGuid(), Guid.NewGuid());

        ConsumptionAiSessionInput aiSession = Assert.Single(command.AiSessions);
        Assert.Empty(aiSession.Items);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenCleanupFails_StillReturnsSuccessAndUpdatesMeal() {
        var userId = UserId.New();
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            imageAssetId: oldAssetId);

        var mealRepository = new SingleMealRepository(meal);
        var cleanup = new RecordingCleanupService("storage_error");
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            mealRepository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var command = new UpdateConsumptionCommand(
            userId.Value,
            meal.Id.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Updated",
            ImageUrl: null,
            newAssetId.Value,
            [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
            [],
            IsNutritionAutoCalculated: false,
            600,
            30,
            20,
            50,
            5,
            0,
            3,
            4);

        Result<ConsumptionModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(mealRepository.UpdateCalled);
        Assert.Equal(newAssetId, meal.ImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenMealTypeInvalid_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var command = new CreateConsumptionCommand(
            userId.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            "NotARealMealType",
            "Created",
            ImageUrl: null,
            ImageAssetId: null,
            [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
            [],
            IsNutritionAutoCalculated: false,
            600,
            30,
            20,
            50,
            5,
            0,
            3,
            4);

        Result<ConsumptionModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal type value.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithValidCommand_PersistsAndRegistersUsage() {
        var user = User.Create("create-consumption@example.com", "hash");
        var repository = new CreatingMealRepository();
        var recentItems = new RecordingRecentItemRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(420, 28, 16, 38, 6, 0)),
            recentItems,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Guid productId = ProductId.New().Value;
        Guid recipeId = RecipeId.New().Value;
        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                "https://cdn.test/meal.png",
                ImageAssetId: null,
                [
                    new ConsumptionItemInput(productId, RecipeId: null, 150),
                    new ConsumptionItemInput(ProductId: null, recipeId, 1),
                ],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.StoredMeal);
        Assert.Equal("Created", repository.StoredMeal.Comment);
        Assert.Equal(2, repository.StoredMeal.Items.Count);
        Assert.True(result.Value.IsNutritionAutoCalculated);
        Assert.Equal(productId, recentItems.LastProductIds.Single().Value);
        Assert.Equal(recipeId, recentItems.LastRecipeIds.Single().Value);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(CreateConsumptionCommand(userId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(CreateConsumptionCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("create-image-failure@example.com", "hash");
        RecordingImageAssetAccessService imageAccess = new RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            imageAccess);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: false,
                ManualCalories: null,
                ManualProteins: 30,
                ManualFats: 20,
                ManualCarbs: 50,
                ManualFiber: 5,
                ManualAlcohol: 0,
                PreMealSatietyLevel: 3,
                PostMealSatietyLevel: 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("ManualCalories", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                Guid.Empty,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(Guid.Empty, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenItemIdentifiersAreMissing_ReturnsValidationFailure() {
        var user = User.Create("create-missing-item-id@example.com", "hash");
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [new ConsumptionItemInput(ProductId: null, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-recipe-id@example.com", "hash");
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [new ConsumptionItemInput(ProductId: null, Guid.Empty, 1)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithInvalidItemOrigin_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-item-origin@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: null, Origin: "Scanner"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal item origin value.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithEmptySourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.Empty, Origin: "AIText"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Source AI item id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-manual-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithRecipeManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-recipe-manual-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [
                new ConsumptionItemInput(ProductId: null, RecipeId.New().Value, 1, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithAiTextItemOrigin_Succeeds() {
        var user = User.Create("create-ai-text-item-origin@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(user.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "AIText"),
            ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithInvalidAiItem_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                    new ConsumptionAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithInvalidAiItemResolution_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-ai-resolution@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Maybe"),
                ])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI item resolution value.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithAiItemResolution_Succeeds() {
        var user = User.Create("create-ai-resolution@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Candidate"),
                ])]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenAiSessionImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("create-session-image-failure@example.com", "hash");
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            new FailingNonNullImageAssetAccessService());

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: ImageAssetId.New().Value)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithEmptyAiSessionImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-ai-image-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: Guid.Empty)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenAiSessionNotesTooLong_ReturnsValidationFailure() {
        var user = User.Create("create-long-ai-notes@example.com", "hash");
        var handler = new CreateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            CreateConsumptionCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(notes: new string('x', 2049))]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Notes", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenAiSourceInvalid_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-ai-source@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, "Scanner", DateTime.UtcNow, Notes: null, [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI recognition source value.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenAiRecognizedAtIsUnspecified_ReturnsValidationFailure() {
        var user = User.Create("create-unspecified-ai-time@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Unspecified), Notes: null, [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecognizedAtUtc timestamp kind must be specified.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WithAiSessionDefaultsSourceAndRecognizedAt_Succeeds() {
        var user = User.Create("create-ai-defaults@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, Source: null, RecognizedAtUtc: null, "recognized", [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Success(result);
        MealAiSession session = Assert.Single(repository.StoredMeal!.AiSessions);
        Assert.Equal(AiRecognitionSource.Text, session.Source);
        Assert.Equal(new StubDateTimeProvider().GetUtcNow().UtcDateTime, session.RecognizedAtUtc);
        Assert.Equal("recognized", session.Notes);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_ReturnsCreatedMealWithoutReloadingBeforeCommit() {
        var user = User.Create("create-no-reload@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: false,
                600,
                30,
                20,
                50,
                5,
                0,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Created", result.Value.Comment);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenAutoNutritionFails_ReturnsServiceErrorWithoutPersisting() {
        var user = User.Create("create-nutrition-failure@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new FailingMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.InvalidData", result.Error.Code);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                Guid.Empty,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId: null, Guid.Empty, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WhenMealIsMissing_ReturnsNotFound() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WhenMealExists_DeletesMeal() {
        var user = User.Create("delete-consumption@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(repository);

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(user.Id.Value, meal.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(meal, repository.DeletedMeal);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryValidator_WithNullUserId_HasInvalidTokenError() {
        var validator = new GetConsumptionsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetConsumptionsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryValidator_WithValidUserId_HasNoErrors() {
        var validator = new GetConsumptionsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetConsumptionsOverviewQuery(Guid.NewGuid(), 1, 10, DateFrom: null, DateTo: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                Guid.NewGuid(),
                Guid.Empty,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);

        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithoutImageChange_DoesNotCleanupExistingMealAsset() {
        var user = User.Create("consumption-owner@example.com", "hash");
        var assetId = ImageAssetId.New();
        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            imageAssetId: assetId);

        var cleanup = new RecordingCleanupService();
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                 4),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenMealTypeInvalid_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-meal-type@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                "Snackish",
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal type value.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAiSourceInvalid_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-ai-source@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, "Scanner", DateTime.UtcNow, Notes: null, [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI recognition source value.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAiRecognizedAtIsUnspecified_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-ai-time@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, "Text", new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Unspecified), Notes: null, [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecognizedAtUtc timestamp kind must be specified.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAutoNutritionFails_ReturnsServiceErrorWithoutPersisting() {
        var user = User.Create("update-nutrition-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            repository,
            new FailingMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.InvalidData", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenUpdatedMealCannotBeReloaded_ReturnsInvalidData() {
        var user = User.Create("update-reload-missing@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new ReloadMissingMealRepository(meal);
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
                [],
                IsNutritionAutoCalculated: false,
                600,
                30,
                20,
                50,
                5,
                0,
                3,
                4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.InvalidData", result.Error.Code);
        Assert.True(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithAiSessionDefaults_UpdatesMealAndCleansOldAsset() {
        var user = User.Create("update-ai-session@example.com", "hash");
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            imageUrl: "https://cdn.example/old.jpg",
            imageAssetId: oldAssetId);
        var repository = new SingleMealRepository(meal);
        var cleanup = new RecordingCleanupService();
        var recentItems = new RecordingRecentItemRepository();
        UpdateConsumptionCommandHandler handler = UpdateConsumptionHandler(
            repository,
            new NoopMealNutritionService(),
            recentItems,
            cleanup,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                newAssetId.Value,
                [],
                [new ConsumptionAiSessionInput(ImageAssetId: null, Source: null, RecognizedAtUtc: null, "generated", [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
                ])],
                IsNutritionAutoCalculated: false,
                120,
                8,
                3,
                16,
                2,
                ManualAlcohol: null,
                2,
                5),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(MealType.Dinner, meal.MealType);
        Assert.Equal(newAssetId, meal.ImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
        Assert.Empty(recentItems.LastProductIds);
        MealAiSession session = Assert.Single(meal.AiSessions);
        Assert.Equal(AiRecognitionSource.Text, session.Source);
        Assert.Equal(new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc), session.RecognizedAtUtc);
        Assert.Single(session.Items);
    }

    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        var handler = new GetConsumptionByIdQueryHandler(CreateConsumptionReadService(new SingleMealRepository(meal)));

        Result<ConsumptionModel> result = await handler.Handle(
            new GetConsumptionByIdQuery(userId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithExistingConsumption_ReturnsMealModel() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            comment: "Owner note");
        meal.AddProduct(ProductId.New(), 150);
        meal.ApplyNutrition(new MealNutritionUpdate(350, 20, 12, 30, 4, 0, IsAutoCalculated: true));

        var handler = new GetConsumptionByIdQueryHandler(CreateConsumptionReadService(new SingleMealRepository(meal)));

        Result<ConsumptionModel> result = await handler.Handle(new GetConsumptionByIdQuery(userId.Value, meal.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(meal.Id.Value, result.Value.Id);
        Assert.Equal("Owner note", result.Value.Comment);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_PreservesDateRangeInstantsForRepositoryQuery() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        var userId = UserId.New();
        var from = new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 5, 19, 59, 59, 999, DateTimeKind.Utc);

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 25, from, to),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(from, repository.LastDateFrom);
        Assert.Equal(to, repository.LastDateTo);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateTo!.Value.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-meal")]
    public async Task GetConsumptionsQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(
                userId.Value,
                1,
                10,
                DateFrom: null,
                DateTo: null,
                MealTypes: ["Lunch", "lunch", "Dinner", "unknown"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([MealType.Lunch, MealType.Dinner], repository.LastMealTypes);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithMeals_ReturnsMappedFavoriteFlags() {
        var user = User.Create("paged-consumptions@example.com", "hash");
        var lunch = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        lunch.ApplyNutrition(new MealNutritionUpdate(420, 24, 14, 44, 5, 0, IsAutoCalculated: true));
        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(610, 38, 20, 58, 7, 0, IsAutoCalculated: true));
        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Evening favorite");
        SetFavoriteMealNavigation(favorite, dinner);
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(
                new RecordingMealPageRepository([lunch, dinner], totalItems: 2),
                new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Data.Count);
        Assert.False(result.Value.Data.Single(item => item.Id == lunch.Id.Value).IsFavorite);
        ConsumptionModel favoriteMeal = result.Value.Data.Single(item => item.Id == dinner.Id.Value);
        Assert.True(favoriteMeal.IsFavorite);
        Assert.Equal(favorite.Id.Value, favoriteMeal.FavoriteMealId);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_ReturnsFavoritePreviewAndFavoriteFlags() {
        var user = User.Create("overview-consumptions@example.com", "hash");
        var breakfast = Meal.Create(user.Id, new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc), MealType.Breakfast);
        breakfast.ApplyNutrition(new MealNutritionUpdate(250, 12, 8, 24, 3, 0, IsAutoCalculated: true));

        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(640, 40, 24, 52, 6, 0, IsAutoCalculated: true));

        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteMealNavigation(favorite, dinner);

        var repository = new RecordingMealPageRepository([breakfast, dinner], totalItems: 2);
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository, new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.AllConsumptions.Data.Count);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.True(result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).FavoriteMealId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-meal")]
    public async Task GetConsumptionsOverviewQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(
                userId.Value,
                1,
                10,
                DateFrom: null,
                DateTo: null,
                MealTypes: ["Breakfast", "breakfast", "Snack", "unknown"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([MealType.Breakfast, MealType.Snack], repository.LastMealTypes);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WithExistingMeal_CopiesItemsAndAppliesNutrition() {
        var user = User.Create("repeat-meal@example.com", "hash");
        var sourceMeal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        sourceMeal.AddProduct(ProductId.New(), 200);
        sourceMeal.AddRecipe(RecipeId.New(), 1);

        var repository = new SingleMealRepository(sourceMeal);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(510, 33, 18, 47, 5, 0)),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedMeal);
        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), repository.LastAddedMeal.Date);
        Assert.Equal(MealType.Dinner, repository.LastAddedMeal.MealType);
        Assert.Equal(2, repository.LastAddedMeal.Items.Count);
        Assert.Equal(510, repository.LastAddedMeal.TotalCalories);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WithAiAndManualNutrition_CopiesFullConsumption() {
        var user = User.Create("repeat-ai-meal@example.com", "hash");
        var staleMealImageAssetId = ImageAssetId.New();
        var aiImageAssetId = ImageAssetId.New();
        var sourceMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 26, 20, 0, 0, DateTimeKind.Utc),
            MealType.Dinner,
            imageUrl: "https://example.com/stale-meal-cover.jpg",
            imageAssetId: staleMealImageAssetId);
        sourceMeal.AddAiSession(
            imageAssetId: aiImageAssetId,
            source: AiRecognitionSource.Photo,
            recognizedAtUtc: new DateTime(2026, 3, 26, 20, 1, 0, DateTimeKind.Utc),
            notes: "photo",
            items: [
                MealAiItemData.Create("Pasta", "ÐŸÐ°ÑÑ‚Ð°", 250, "g", 420, 14, 8, 72, 4, 0),
            ]);
        sourceMeal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 430,
            TotalProteins: 15,
            TotalFats: 9,
            TotalCarbs: 73,
            TotalFiber: 4,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: 430,
            ManualProteins: 15,
            ManualFats: 9,
            ManualCarbs: 73,
            ManualFiber: 4,
            ManualAlcohol: 0));

        var repository = new SingleMealRepository(sourceMeal);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(0, 0, 0, 0, 0, 0)),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 19, 30, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedMeal);
        Assert.Single(repository.LastAddedMeal.AiSessions);
        Assert.Single(repository.LastAddedMeal.AiSessions.Single().Items);
        Assert.Equal("Pasta", repository.LastAddedMeal.AiSessions.Single().Items.Single().NameEn);
        Assert.Equal(aiImageAssetId, repository.LastAddedMeal.AiSessions.Single().ImageAssetId);
        Assert.Null(repository.LastAddedMeal.ImageUrl);
        Assert.Null(repository.LastAddedMeal.ImageAssetId);
        Assert.False(repository.LastAddedMeal.IsNutritionAutoCalculated);
        Assert.Equal(430, repository.LastAddedMeal.TotalCalories);
        Assert.Equal(430, repository.LastAddedMeal.ManualCalories);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-image-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        RecordingImageAssetAccessService imageAccess = new RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user, imageAccess: imageAccess);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var user = User.Create("update-missing-user@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(userId: null, meal.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenSatietyInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-satiety-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, preMealSatietyLevel: -1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenItemIdentifiersAreMissing_ReturnsValidationFailure() {
        var user = User.Create("update-missing-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [new ConsumptionItemInput(ProductId: null, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-product-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [new ConsumptionItemInput(Guid.Empty, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithInvalidItemOrigin_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-item-origin@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: null, Origin: "Scanner"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal item origin value.", result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptySourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.Empty, Origin: "AIText"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Source AI item id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-manual-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithRecipeManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-recipe-manual-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [
                new ConsumptionItemInput(ProductId: null, RecipeId.New().Value, 1, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithAiTextItemOrigin_Succeeds() {
        var user = User.Create("update-ai-text-item-origin@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [
                new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "AIText"),
            ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithRecipeItem_RegistersRecipeUsage() {
        var user = User.Create("update-recipe-item@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        var recentItems = new RecordingRecentItemRepository();
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user, recentItems: recentItems);
        var recipeId = RecipeId.New();

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, meal.Id.Value, items: [new ConsumptionItemInput(ProductId: null, recipeId.Value, 1)]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(recipeId, recentItems.LastRecipeIds.Single());
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyAiSessionImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-ai-image-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: Guid.Empty)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAiSessionImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-session-image-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user, imageAccess: new FailingNonNullImageAssetAccessService());

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: ImageAssetId.New().Value)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAiSessionNotesTooLong_ReturnsValidationFailure() {
        var user = User.Create("update-long-ai-notes@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(notes: new string('x', 2049))]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenManualNutritionInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-manual-nutrition-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                isNutritionAutoCalculated: false,
                manualCalories: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WhenAiItemInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-ai-item@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [new ConsumptionAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithInvalidAiItemResolution_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-ai-resolution@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Maybe"),
                ])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI item resolution value.", result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithAiItemResolution_Succeeds() {
        var user = User.Create("update-ai-resolution@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Candidate"),
                ])]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(UserId: null, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-repeat-meal@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WhenSourceMealMissing_ReturnsNotFound() {
        var user = User.Create("repeat-missing-source@example.com", "hash");
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionByIdQueryHandler(CreateConsumptionReadService(new CreatingMealRepository()));

        Result<ConsumptionModel> result = await handler.Handle(
            new GetConsumptionByIdQuery(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-overview-consumptions@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public void ConsumptionMappings_ToPagedResponse_MapsItemsAndPagination() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);

        var response = (Items: (IReadOnlyList<Meal>)[meal], TotalItems: 25).ToPagedResponse(page: 2, limit: 10);

        Assert.Single(response.Data);
        Assert.Equal(meal.Id.Value, response.Data[0].Id);
        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.Limit);
        Assert.Equal(3, response.TotalPages);
        Assert.Equal(25, response.TotalItems);
    }

    private static CreateConsumptionCommand CreateConsumptionCommand(
        Guid? userId,
        Guid? imageAssetId = null,
        IReadOnlyList<ConsumptionItemInput>? items = null,
        IReadOnlyList<ConsumptionAiSessionInput>? aiSessions = null) =>
        new(
            userId,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Created",
            ImageUrl: null,
            imageAssetId,
            items ?? [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
            aiSessions ?? [],
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            3,
            4);

    private static UpdateConsumptionCommand UpdateConsumptionCommand(
        Guid? userId,
        Guid consumptionId,
        Guid? imageAssetId = null,
        IReadOnlyList<ConsumptionItemInput>? items = null,
        IReadOnlyList<ConsumptionAiSessionInput>? aiSessions = null,
        bool isNutritionAutoCalculated = true,
        double? manualCalories = 600,
        int preMealSatietyLevel = 3) =>
        new(
            userId,
            consumptionId,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Updated",
            ImageUrl: null,
            imageAssetId,
            items ?? [new ConsumptionItemInput(ProductId.New().Value, RecipeId: null, 150)],
            aiSessions ?? [],
            isNutritionAutoCalculated,
            manualCalories,
            30,
            20,
            50,
            5,
            0,
            preMealSatietyLevel,
            4);

    private static ConsumptionAiSessionInput ValidAiSession(
        Guid? imageAssetId = null,
        string? notes = null,
        IReadOnlyList<ConsumptionAiItemInput>? items = null) =>
        new(
            imageAssetId,
            "Text",
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            notes,
            items ?? [new ConsumptionAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0)]);

    private static UpdateConsumptionCommandHandler CreateUpdateHandler(
        IMealRepository repository,
        User user,
        RecordingRecentItemRepository? recentItems = null,
        IImageAssetAccessService? imageAccess = null) =>
        UpdateConsumptionHandler(
            repository,
            new NoopMealNutritionService(),
            recentItems ?? new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            imageAccess ?? FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class FailingNonNullImageAssetAccessService : IImageAssetAccessService {
        public Task<Result<ImageAsset?>> ResolveOptionalAsync(
            ImageAssetId? assetId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                assetId.HasValue
                    ? Result.Failure<ImageAsset?>(Errors.Image.Forbidden())
                    : Result.Success<ImageAsset?>(value: null));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleMealRepository(Meal meal) : IMealRepository {
        public bool UpdateCalled { get; private set; }
        public Meal? LastAddedMeal { get; private set; }
        public Meal? DeletedMeal { get; private set; }

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
            LastAddedMeal = meal;
            return Task.FromResult(meal);
        }

        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) {
            DeletedMeal = meal;
            return Task.CompletedTask;
        }

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FindById(id, userId));

        private Meal? FindById(MealId id, UserId userId) {
            if (LastAddedMeal is not null && LastAddedMeal.Id == id && LastAddedMeal.UserId == userId) {
                return LastAddedMeal;
            }

            return id == meal.Id && userId == meal.UserId ? meal : null;
        }

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetTotalMealCountAsync(
            UserId userId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId, DateTime date,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReloadMissingMealRepository(Meal meal) : IMealRepository {
        private bool _reloadStarted;
        public bool UpdateCalled { get; private set; }

        public Task<Meal> AddAsync(Meal addedMeal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Meal updatedMeal, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            _reloadStarted = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Meal deletedMeal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Meal?>(_reloadStarted ? null : meal);

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetTotalMealCountAsync(
            UserId userId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId, DateTime date,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class CreatingMealRepository : IMealRepository {
        public Meal? StoredMeal { get; private set; }

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
            StoredMeal = meal;
            return Task.FromResult(meal);
        }

        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Meal?>(StoredMeal is not null && StoredMeal.Id == id && StoredMeal.UserId == userId ? StoredMeal : null);

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetTotalMealCountAsync(
            UserId userId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId, DateTime date,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingMealPageRepository(
        IReadOnlyList<Meal>? items = null,
        int totalItems = 0) : IMealRepository {
        private readonly IReadOnlyList<Meal> _items = items ?? [];
        public DateTime? LastDateFrom { get; private set; }
        public DateTime? LastDateTo { get; private set; }
        public IReadOnlyCollection<MealType>? LastMealTypes { get; private set; }

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) {
            LastDateFrom = filters.DateFrom;
            LastDateTo = filters.DateTo;
            LastMealTypes = filters.MealTypes;
            return Task.FromResult((_items, totalItems));
        }

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetTotalMealCountAsync(
            UserId userId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(
            UserId userId, DateTime date,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopMealNutritionService : IMealNutritionService {
        public Task<Result<MealNutritionSummary>> CalculateAsync(
            Meal meal,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new MealNutritionSummary(0, 0, 0, 0, 0, 0)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedMealNutritionService(MealNutritionSummary nutritionSummary) : IMealNutritionService {
        public Task<Result<MealNutritionSummary>> CalculateAsync(
            Meal meal,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(nutritionSummary));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingMealNutritionService : IMealNutritionService {
        public Task<Result<MealNutritionSummary>> CalculateAsync(
            Meal meal,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Failure<MealNutritionSummary>(
                Errors.Consumption.InvalidData("Nutrition calculation failed.")));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRecentItemRepository : IRecentItemRepository {
        public IReadOnlyList<ProductId> LastProductIds { get; private set; } = [];
        public IReadOnlyList<RecipeId> LastRecipeIds { get; private set; } = [];

        public Task RegisterUsageAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) {
            LastProductIds = productIds.ToList();
            LastRecipeIds = recipeIds.ToList();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    private static IConsumptionReadService CreateConsumptionReadService(
        IMealReadRepository mealRepository,
        IFavoriteMealReadRepository? favoriteMealRepository = null) =>
        new ConsumptionReadService(mealRepository, favoriteMealRepository ?? new StubFavoriteMealRepository());

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) =>
        new StubCurrentUserAccessService(user);

    [ExcludeFromCodeCoverage]
    private sealed class StubCurrentUserAccessService(User user) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Error? error = user switch {
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                _ => null,
            };

            return Task.FromResult(error);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFavoriteMealRepository(params FavoriteMeal[] favorites) : IFavoriteMealRepository {
        private readonly IReadOnlyList<FavoriteMeal> _favorites = favorites;

        public Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
        public Task<FavoriteMeal?> GetByIdAsync(FavoriteMealId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<FavoriteMeal?>(null);
        public Task<FavoriteMeal?> GetByMealIdAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.FirstOrDefault(x => x.MealId == mealId));
        public Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
            UserId userId,
            IReadOnlyCollection<MealId> mealIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<MealId, FavoriteMeal>>(_favorites.Where(x => mealIds.Contains(x.MealId)).ToDictionary(x => x.MealId));
    }

    private static void SetFavoriteMealNavigation(FavoriteMeal favorite, Meal meal) {
        typeof(FavoriteMeal)
            .GetProperty(nameof(FavoriteMeal.Meal))!
            .SetValue(favorite, meal);
    }
}
