using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

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
            7);

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
            7);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Unknown meal type value.", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateConsumptionCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new CreatingMealRepository();
        var handler = new CreateConsumptionCommandHandler(
            repository,
            new NoopMealNutritionService(),
            new RecordingRecentItemRepository(),
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
                PostMealSatietyLevel: 7),
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
                7),
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
                7),
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
                7),
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
                7),
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
                7),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task GetConsumptionsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsQueryHandler(new CreatingMealRepository());

        var result = await handler.Handle(
            new GetConsumptionsQuery(null, 1, 10, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetConsumptionsQueryHandler_NormalizesDateRangeToUtcForRepositoryQuery() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(repository);
        var userId = UserId.New();
        var localFrom = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Local);
        var localTo = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Local);

        var result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 25, localFrom, localTo),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizeUtcDate(localFrom), repository.LastDateFrom);
        Assert.Equal(NormalizeUtcDate(localTo), repository.LastDateTo);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateTo!.Value.Kind);
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    private sealed class SingleMealRepository(Meal meal) : IMealRepository {
        public bool UpdateCalled { get; private set; }

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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
            Task.FromResult<Meal?>(id == meal.Id && userId == meal.UserId ? meal : null);

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
    }

    private sealed class CreatingMealRepository : IMealRepository {
        private Meal? _storedMeal;

        public Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default) {
            _storedMeal = meal;
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
            Task.FromResult<Meal?>(_storedMeal is not null && _storedMeal.Id == id && _storedMeal.UserId == userId ? _storedMeal : null);

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
    }

    private sealed class RecordingMealPageRepository : IMealRepository {
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
            return Task.FromResult(((IReadOnlyList<Meal>)[], 0));
        }

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
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

    private sealed class RecordingRecentItemRepository : IRecentItemRepository {
        public Task RegisterUsageAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

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
}
