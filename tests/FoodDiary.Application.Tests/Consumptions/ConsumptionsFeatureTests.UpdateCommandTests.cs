using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Tests.Consumptions;

public partial class ConsumptionsFeatureTests {

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
    public async Task UpdateConsumptionCommandHandler_WithoutManualOrAiItems_ReturnsRequiredItemsFailure() {
        var user = User.Create("update-without-items@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(new SingleMealRepository(meal), user);

        Result<ConsumptionModel> result = await handler.Handle(
            new UpdateConsumptionCommand(
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
    public async Task UpdateConsumptionCommandHandler_WhenMealMissing_ReturnsNotFound() {
        var user = User.Create("update-missing-meal@example.com", "hash");
        UpdateConsumptionCommandHandler handler = CreateUpdateHandler(new CreatingMealRepository(), user);
        var missingMealId = Guid.NewGuid();

        Result<ConsumptionModel> result = await handler.Handle(
            UpdateConsumptionCommand(user.Id.Value, missingMealId),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.NotFound", result.Error.Code);
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

}
