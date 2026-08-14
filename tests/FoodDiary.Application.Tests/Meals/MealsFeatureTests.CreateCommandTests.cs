using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Tests.Meals;

public partial class MealsFeatureTests {

    [Fact]
    public async Task CreateMealCommandHandler_WhenMealTypeInvalid_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        var command = new CreateMealCommand(
            userId.Value,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            "NotARealMealType",
            "Created",
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
            4);

        Result<MealModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal type value.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithValidCommand_PersistsAndRegistersUsage() {
        var user = User.Create("create-meal@example.com", "hash");
        var repository = new CreatingMealRepository();
        var recentItems = new RecordingRecentItemRepository();
        IAchievementEvaluationOutbox achievementOutbox = Substitute.For<IAchievementEvaluationOutbox>();
        var handler = new CreateMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(420, 28, 16, 38, 6, 0)),
            recentItems,
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance,
            achievementOutbox);

        Guid productId = ProductId.New().Value;
        Guid recipeId = RecipeId.New().Value;
        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                "https://cdn.test/meal.png",
                ImageAssetId: null,
                [
                    new MealItemInput(productId, RecipeId: null, 150),
                    new MealItemInput(ProductId: null, recipeId, 1),
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
        await achievementOutbox.Received(1).EnqueueAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(CreateMealCommand(userId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-create-meal@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(CreateMealCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("create-image-failure@example.com", "hash");
        RecordingImageAssetAccessService imageAccess = new RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.NotFound(Guid.NewGuid()));
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            imageAccess);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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
    public async Task CreateMealCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
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
    public async Task CreateMealCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemInput(Guid.Empty, RecipeId: null, 150)],
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
    public async Task CreateMealCommandHandler_WhenItemIdentifiersAreMissing_ReturnsValidationFailure() {
        var user = User.Create("create-missing-item-id@example.com", "hash");
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [new MealItemInput(ProductId: null, RecipeId: null, 150)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-recipe-id@example.com", "hash");
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [new MealItemInput(ProductId: null, Guid.Empty, 1)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithInvalidItemOrigin_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-item-origin@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: null, Origin: "Scanner"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal item origin value.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithEmptySourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.Empty, Origin: "AiText"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Source AI item id", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-manual-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithRecipeManualOriginAndSourceAiItemId_ReturnsValidationFailure() {
        var user = User.Create("create-recipe-manual-source-ai-item-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [
                new MealItemInput(ProductId: null, RecipeId.New().Value, 1, SourceAiItemId: Guid.NewGuid(), Origin: "Manual"),
            ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("manual meal item", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithAiTextItemOrigin_Succeeds() {
        var user = User.Create("create-ai-text-item-origin@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(user.Id.Value, items: [
                new MealItemInput(ProductId.New().Value, RecipeId: null, 150, SourceAiItemId: Guid.NewGuid(), Origin: "AiText"),
            ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithInvalidAiItem_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                userId.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new MealAiSessionInput(ImageAssetId: null, "Text", DateTime.UtcNow, Notes: null, [
                    new MealAiItemInput("", NameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0),
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
    public async Task CreateMealCommandHandler_WithInvalidAiItemResolution_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-ai-resolution@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Maybe"),
                ])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown AI item resolution value.", result.Error.Message, StringComparison.Ordinal);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithAiItemResolution_Succeeds() {
        var user = User.Create("create-ai-resolution@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(items: [
                    new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0, Resolution: "Candidate"),
                ])]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenAiSessionImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("create-session-image-failure@example.com", "hash");
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            new FailingNonNullImageAssetAccessService());

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: ImageAssetId.New().Value)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithEmptyAiSessionImageAssetId_ReturnsValidationFailure() {
        var user = User.Create("create-empty-ai-image-id@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(imageAssetId: Guid.Empty)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenAiSessionNotesTooLong_ReturnsValidationFailure() {
        var user = User.Create("create-long-ai-notes@example.com", "hash");
        var handler = new CreateMealCommandHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            CreateMealCommand(
                user.Id.Value,
                items: [],
                aiSessions: [ValidAiSession(notes: new string('x', 2049))]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Notes", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenAiSourceInvalid_ReturnsValidationFailure() {
        var user = User.Create("create-invalid-ai-source@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
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
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenAiRecognizedAtIsUnspecified_ReturnsValidationFailure() {
        var user = User.Create("create-unspecified-ai-time@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
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
        Assert.Null(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WithAiSessionDefaultsSourceAndRecognizedAt_Succeeds() {
        var user = User.Create("create-ai-defaults@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(120, 8, 3, 16, 2, 0)),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
                ImageUrl: null,
                ImageAssetId: null,
                [],
                [new MealAiSessionInput(ImageAssetId: null, Source: null, RecognizedAtUtc: null, "recognized", [
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

        ResultAssert.Success(result);
        MealAiSession session = Assert.Single(repository.StoredMeal!.AiSessions);
        Assert.Equal(AiRecognitionSource.Text, session.Source);
        Assert.Equal(new StubDateTimeProvider().GetUtcNow().UtcDateTime, session.RecognizedAtUtc);
        Assert.Equal("recognized", session.Notes);
    }

    [Fact]
    public async Task CreateMealCommandHandler_ReturnsCreatedMealWithoutReloadingBeforeCommit() {
        var user = User.Create("create-no-reload@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
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

        ResultAssert.Success(result);
        Assert.Equal("Created", result.Value.Comment);
        Assert.NotNull(repository.StoredMeal);
    }

    [Fact]
    public async Task CreateMealCommandHandler_WhenAutoNutritionFails_ReturnsServiceErrorWithoutPersisting() {
        var user = User.Create("create-nutrition-failure@example.com", "hash");
        var repository = new CreatingMealRepository();
        var handler = new CreateMealCommandHandler(
            repository,
            new FailingMealNutritionService(),
            new RecordingRecentItemRepository(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

        Result<MealModel> result = await handler.Handle(
            new CreateMealCommand(
                user.Id.Value,
                new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
                MealType.Dinner.ToString(),
                "Created",
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
        Assert.Null(repository.StoredMeal);
    }

}
