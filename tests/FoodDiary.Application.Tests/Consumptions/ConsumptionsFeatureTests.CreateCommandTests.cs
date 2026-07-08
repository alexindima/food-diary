using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Tests.Consumptions;

public partial class ConsumptionsFeatureTests {

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

}
