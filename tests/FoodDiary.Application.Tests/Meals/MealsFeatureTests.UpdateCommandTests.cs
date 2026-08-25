using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Commands.UpdateMeal;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Tests.Meals;

public partial class MealsFeatureTests {

    [Fact]
    public async Task UpdateMealCommandHandler_WhenCleanupFails_StillReturnsSuccessAndUpdatesMeal() {
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
        UpdateMealCommandHandler handler = UpdateMealHandler(
            mealRepository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        var command = new UpdateMealCommand(
            userId.Value,
            meal.Id.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Updated",
            ImageUrl: null,
            newAssetId.Value,
            [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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

        Result<MealModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(mealRepository.UpdateCalled);
        Assert.Equal(newAssetId, meal.ImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                Guid.Empty,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
    public async Task UpdateMealCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                userId.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId: null, Guid.Empty, 150)],
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
    public async Task UpdateMealCommandHandler_WithEmptyMealId_ReturnsValidationFailure() {
        UpdateMealCommandHandler handler = UpdateMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                Guid.NewGuid(),
                Guid.Empty,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
        Assert.Contains("MealId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithoutManualOrAiItems_ReturnsRequiredItemsFailure() {
        var user = User.Create("update-without-items@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateMealCommandHandler handler = CreateUpdateHandler(new SingleMealRepository(meal), user);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                Items: null!,
                AiSessions: null!,
                IsNutritionAutoCalculated: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                PreMealSatietyLevel: 3,
                PostMealSatietyLevel: 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("Items", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WhenMealMissing_ReturnsNotFound() {
        var user = User.Create("update-missing-meal@example.com", "hash");
        UpdateMealCommandHandler handler = CreateUpdateHandler(new CreatingMealRepository(), user);
        var missingMealId = Guid.NewGuid();

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, missingMealId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Meal.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-meal@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);

        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
    public async Task UpdateMealCommandHandler_WithoutImageChange_DoesNotCleanupExistingMealAsset() {
        var user = User.Create("meal-owner@example.com", "hash");
        var assetId = ImageAssetId.New();
        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            imageAssetId: assetId);

        var cleanup = new RecordingCleanupService();
        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            cleanup,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
    public async Task UpdateMealCommandHandler_WhenMealTypeInvalid_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-meal-type@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                "Snackish",
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
    public async Task UpdateMealCommandHandler_WhenAiSourceInvalid_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-ai-source@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new MealAiSessionInput(ImageAssetId: null, "Scanner", DateTime.UtcNow, Notes: null, [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
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
    public async Task UpdateMealCommandHandler_WhenAiRecognizedAtIsUnspecified_ReturnsValidationFailure() {
        var user = User.Create("invalid-update-ai-time@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateMealCommandHandler handler = UpdateMealHandler(
            new SingleMealRepository(meal),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new MealAiSessionInput(ImageAssetId: null, "Text", new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Unspecified), Notes: null, [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
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
    public async Task UpdateMealCommandHandler_WhenAutoNutritionFails_ReturnsServiceErrorWithoutPersisting() {
        var user = User.Create("update-nutrition-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = UpdateMealHandler(
            repository,
            new FailingMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
        Assert.Equal("Meal.InvalidData", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WhenUpdatedMealCannotBeReloaded_ReturnsInvalidData() {
        var user = User.Create("update-reload-missing@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new ReloadMissingMealRepository(meal);
        UpdateMealCommandHandler handler = UpdateMealHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
        Assert.Equal("Meal.InvalidData", result.Error.Code);
        Assert.True(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithAiSessionDefaults_UpdatesMealAndCleansOldAsset() {
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
        UpdateMealCommandHandler handler = UpdateMealHandler(
            repository,
            new NoopMealNutritionService(),
            recentItems,
            cleanup,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Updated",
                ImageUrl: null,
                newAssetId.Value,
                [],
                [new MealAiSessionInput(ImageAssetId: null, Source: null, RecognizedAtUtc: null, "generated", [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0),
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
    public async Task UpdateMealCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-image-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        RecordingImageAssetAccessService imageAccess = new RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user, imageAccess: imageAccess);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var user = User.Create("update-missing-user@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(userId: null, meal.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WhenSatietyInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-satiety-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, preMealSatietyLevel: -1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WhenItemIdentifiersAreMissing_ReturnsValidationFailure() {
        var user = User.Create("update-missing-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [new MealItemInput(ProductId: null, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-product-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [new MealItemInput(Guid.Empty, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithInvalidItemOrigin_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-item-origin@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: null, Origin: "Scanner"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal item origin value.", result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithEmptySourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.Empty, Origin: "AiText"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Source AI item id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-manual-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithRecipeManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("update-recipe-manual-source-ai-item-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [
                new MealItemInput(ProductId: null, RecipeId.New().Value, 1, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithCallerProvidedAiProvenance_ReturnsValidationFailure() {
        var user = User.Create("update-ai-text-item-origin@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "AiText"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("assigned by the server", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithRecipeItem_RegistersRecipeUsage() {
        var user = User.Create("update-recipe-item@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        var recentItems = new RecordingRecentItemRepository();
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user, recentItems: recentItems);
        var recipeId = RecipeId.New();

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(user.Id.Value, meal.Id.Value, items: [new MealItemInput(ProductId: null, recipeId.Value, 1)]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(recipeId, recentItems.LastRecipeIds.Single());
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithEmptyAiSessionImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-ai-image-id@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
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
    public async Task UpdateMealCommandHandler_WhenAiSessionImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-session-image-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user, imageAccess: new FailingNonNullImageAssetAccessService());

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
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
    public async Task UpdateMealCommandHandler_WhenAiSessionNotesTooLong_ReturnsValidationFailure() {
        var user = User.Create("update-long-ai-notes@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
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
    public async Task UpdateMealCommandHandler_WhenManualNutritionInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-manual-nutrition-failure@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
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
    public async Task UpdateMealCommandHandler_WhenAiItemInvalid_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-ai-item@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [new MealAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithInvalidAiItemResolution_ReturnsValidationFailure() {
        var user = User.Create("update-invalid-ai-resolution@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Maybe"),
                ])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI item resolution value.", result.Error.Message, StringComparison.Ordinal);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateMealCommandHandler_WithAiItemResolution_Succeeds() {
        var user = User.Create("update-ai-resolution@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        UpdateMealCommandHandler handler = CreateUpdateHandler(repository, user);

        Result<MealModel> result = await handler.Handle(
            UpdateMealCommand(
                user.Id.Value,
                meal.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Candidate"),
                ])]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
    }

}
