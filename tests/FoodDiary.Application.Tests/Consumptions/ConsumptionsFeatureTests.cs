using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
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

namespace FoodDiary.Application.Tests.Consumptions;

public class ConsumptionsFeatureTests {
    [Fact]
    public void ConsumptionItemValidator_WhenIdsAreMissing_Fails() {
        var result = ConsumptionItemValidator.Validate(new ConsumptionItemInput(null, null, 100));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void ManualNutritionValidator_WhenAlcoholIsNull_DefaultsToZero() {
        var result = ManualNutritionValidator.Validate(100, 10, 5, 20, 3, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Alcohol);
    }

    [Fact]
    public void SatietyLevelValidator_WhenPreMealOutOfRange_UsesContractFieldName() {
        var result = SatietyLevelValidator.Validate(-1, 5);

        Assert.True(result.IsFailure);
        Assert.Contains("PreMealSatietyLevel", result.Error.Message);
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
                MealAiItemData.Create("Banana", null, 100, "g", 89, 1.1, 0.3, 23, 2.6, 0)
            ]);

        var result = MealNutritionCalculator.Calculate(
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
    public void ConsumptionHttpMappings_CreateToCommand_WhenListsAreNull_MapsEmptyCollections() {
        var request = new CreateConsumptionHttpRequest(
            DateTime.UtcNow,
            MealType.Breakfast.ToString(),
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            Items: null!,
            AiSessions: null);

        var command = request.ToCommand(Guid.NewGuid());

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
                new ConsumptionItemHttpRequest(ProductId.New().Value, null, 150)
            ],
            AiSessions: [
                new ConsumptionAiSessionHttpRequest(
                    ImageAssetId: null,
                    Source: "Text",
                    RecognizedAtUtc: DateTime.UtcNow,
                    Notes: null,
                    Items: null!)
            ]);

        var command = request.ToCommand(Guid.NewGuid(), Guid.NewGuid());

        var aiSession = Assert.Single(command.AiSessions);
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
        var handler = new UpdateConsumptionCommandHandler(
            mealRepository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var command = new UpdateConsumptionCommand(
            userId.Value,
            meal.Id.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Updated",
            null,
            newAssetId.Value,
            [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
            [],
            false,
            600,
            30,
            20,
            50,
            5,
            0,
            3,
            4);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var command = new CreateConsumptionCommand(
            userId.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            "NotARealMealType",
            "Created",
            null,
            null,
            [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
            [],
            false,
            600,
            30,
            20,
            50,
            5,
            0,
            3,
            4);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(user),
            new StubDateTimeProvider());

        var productId = ProductId.New().Value;
        var recipeId = RecipeId.New().Value;
        var result = await handler.Handle(
            new CreateConsumptionCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                "https://cdn.test/meal.png",
                null,
                [
                    new ConsumptionItemInput(productId, null, 150),
                    new ConsumptionItemInput(null, recipeId, 1)
                ],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.StoredMeal);
        Assert.Equal("Created", repository.StoredMeal.Comment);
        Assert.Equal(2, repository.StoredMeal.Items.Count);
        Assert.True(result.Value.IsNutritionAutoCalculated);
        Assert.Equal(productId, recentItems.LastProductIds.Single().Value);
        Assert.Equal(recipeId, recentItems.LastRecipeIds.Single().Value);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                null,
                null,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                false,
                ManualCalories: null,
                ManualProteins: 30,
                ManualFats: 20,
                ManualCarbs: 50,
                ManualFiber: 5,
                ManualAlcohol: 0,
                PreMealSatietyLevel: 3,
                PostMealSatietyLevel: 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                null,
                Guid.Empty,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new CreateConsumptionCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                null,
                null,
                [new ConsumptionItemInput(Guid.Empty, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        var handler = new UpdateConsumptionCommandHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new UpdateConsumptionCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                null,
                Guid.Empty,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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

        var handler = new UpdateConsumptionCommandHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new UpdateConsumptionCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                null,
                null,
                [new ConsumptionItemInput(null, Guid.Empty, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        var handler = new DeleteConsumptionCommandHandler(new CreatingMealRepository());

        var result = await handler.Handle(
            new DeleteConsumptionCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        var handler = new UpdateConsumptionCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new UpdateConsumptionCommand(
                Guid.NewGuid(),
                Guid.Empty,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                null,
                null,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateConsumptionCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);

        var handler = new UpdateConsumptionCommandHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                null,
                null,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
        var handler = new UpdateConsumptionCommandHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            new StubUserRepository(user),
            new StubDateTimeProvider());

        var result = await handler.Handle(
            new UpdateConsumptionCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                null,
                null,
                [new ConsumptionItemInput(ProductId.New().Value, null, 150)],
                [],
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                3,
                 4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        var handler = new GetConsumptionByIdQueryHandler(new SingleMealRepository(meal));

        var result = await handler.Handle(
            new GetConsumptionByIdQuery(userId.Value, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
        meal.ApplyNutrition(new MealNutritionUpdate(350, 20, 12, 30, 4, 0, true));

        var handler = new GetConsumptionByIdQueryHandler(new SingleMealRepository(meal));

        var result = await handler.Handle(new GetConsumptionByIdQuery(userId.Value, meal.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(meal.Id.Value, result.Value.Id);
        Assert.Equal("Owner note", result.Value.Comment);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsQueryHandler(
            new CreatingMealRepository(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubFavoriteMealRepository());

        var result = await handler.Handle(
            new GetConsumptionsQuery(null, 1, 10, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_PreservesDateRangeInstantsForRepositoryQuery() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            repository,
            new StubUserRepository(User.Create("user@example.com", "hash")),
            new StubFavoriteMealRepository());
        var userId = UserId.New();
        var from = new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 5, 19, 59, 59, 999, DateTimeKind.Utc);

        var result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 25, from, to),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(from, repository.LastDateFrom);
        Assert.Equal(to, repository.LastDateTo);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateTo!.Value.Kind);
    }

    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_ReturnsFavoritePreviewAndFavoriteFlags() {
        var user = User.Create("overview-consumptions@example.com", "hash");
        var breakfast = Meal.Create(user.Id, new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc), MealType.Breakfast);
        breakfast.ApplyNutrition(new MealNutritionUpdate(250, 12, 8, 24, 3, 0, true));

        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(640, 40, 24, 52, 6, 0, true));

        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteMealNavigation(favorite, dinner);

        var repository = new RecordingMealPageRepository([breakfast, dinner], totalItems: 2);
        var handler = new GetConsumptionsOverviewQueryHandler(
            repository,
            new StubUserRepository(user),
            new StubFavoriteMealRepository([favorite]));

        var result = await handler.Handle(
            new GetConsumptionsOverviewQuery(user.Id.Value, 1, 10, null, null, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.AllConsumptions.Data.Count);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.True(result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).FavoriteMealId);
    }

    [Fact]
    public async Task RepeatMealCommandHandler_WithExistingMeal_CopiesItemsAndAppliesNutrition() {
        var user = User.Create("repeat-meal@example.com", "hash");
        var sourceMeal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        sourceMeal.AddProduct(ProductId.New(), 200);
        sourceMeal.AddRecipe(RecipeId.New(), 1);

        var repository = new SingleMealRepository(sourceMeal);
        var handler = new RepeatMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(510, 33, 18, 47, 5, 0)),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
                MealAiItemData.Create("Pasta", "Паста", 250, "g", 420, 14, 8, 72, 4, 0)
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
        var handler = new RepeatMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(0, 0, 0, 0, 0, 0)),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 19, 30, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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

    private sealed class SingleMealRepository(Meal meal) : IMealRepository {
        public bool UpdateCalled { get; private set; }
        public Meal? LastAddedMeal { get; private set; }

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
            LastAddedMeal = meal;
            return Task.FromResult(meal);
        }

        public Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Meal?> GetByIdAsync(
            MealId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Meal?>(
                LastAddedMeal is not null && LastAddedMeal.Id == id && LastAddedMeal.UserId == userId
                    ? LastAddedMeal
                    : id == meal.Id && userId == meal.UserId
                        ? meal
                        : null);

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? dateFrom,
            DateTime? dateTo,
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
            DateTime? dateFrom,
            DateTime? dateTo,
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

    private sealed class RecordingMealPageRepository(
        IReadOnlyList<Meal>? items = null,
        int totalItems = 0) : IMealRepository {
        private readonly IReadOnlyList<Meal> _items = items ?? [];
        public DateTime? LastDateFrom { get; private set; }
        public DateTime? LastDateTo { get; private set; }

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
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken = default) {
            LastDateFrom = dateFrom;
            LastDateTo = dateTo;
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

    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    private sealed class NoopRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());
    }

    private sealed class NoopMealNutritionService : IMealNutritionService {
        public Task<Result<MealNutritionSummary>> CalculateAsync(
            Meal meal,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(new MealNutritionSummary(0, 0, 0, 0, 0, 0)));
    }

    private sealed class FixedMealNutritionService(MealNutritionSummary nutritionSummary) : IMealNutritionService {
        public Task<Result<MealNutritionSummary>> CalculateAsync(
            Meal meal,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(nutritionSummary));
    }

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

    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => new(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetConsumptionsQueryHandler(
            new CreatingMealRepository(),
            new StubUserRepository(user),
            new StubFavoriteMealRepository());

        var result = await handler.Handle(
            new GetConsumptionsQuery(user.Id.Value, 1, 10, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubFavoriteMealRepository : IFavoriteMealRepository {
        private readonly IReadOnlyList<FavoriteMeal> _favorites;

        public StubFavoriteMealRepository(params FavoriteMeal[] favorites) {
            _favorites = favorites;
        }

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
