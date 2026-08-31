using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Commands.DeleteMeal;
using FoodDiary.Application.Meals.Commands.UpdateMeal;
using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Commands.RepeatMeal;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Favorites.FavoriteMeals.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Recipes.Common;

namespace FoodDiary.Application.Tests.Meals;

[ExcludeFromCodeCoverage]
public partial class MealsFeatureTests {
    private static UpdateMealCommandHandler UpdateMealHandler(
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

    private static DeleteMealCommandHandler DeleteMealHandler(IMealRepository repository) =>
        new(repository, repository, Substitute.For<ICurrentUserAccessService>());

    private static RepeatMealCommandHandler RepeatMealHandler(
        IMealRepository repository,
        IMealNutritionService mealNutritionService,
        ICurrentUserAccessService currentUserAccessService,
        IAchievementEvaluationOutbox? achievementOutbox = null) =>
        new(repository, repository, mealNutritionService, currentUserAccessService,
            achievementOutbox ?? Substitute.For<IAchievementEvaluationOutbox>());

    private static MealProjectionReadModel CreateReadModelWithAiItem(Guid mealId, Guid sessionId, Guid aiItemId) =>
        new(
            mealId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            TotalCalories: 120,
            TotalProteins: 8,
            TotalFats: 3,
            TotalCarbs: 16,
            TotalFiber: 2,
            TotalAlcohol: 0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            PreMealSatietyLevel: 0,
            PostMealSatietyLevel: 0,
            Items: [],
            AiSessions: [CreateAiSessionReadModel(mealId, sessionId, aiItemId)]);

    private static MealAiSessionProjectionReadModel CreateAiSessionReadModel(Guid mealId, Guid sessionId, Guid aiItemId) =>
        new(
            sessionId,
            mealId,
            ImageAssetId: Guid.NewGuid(),
            ImageUrl: "https://cdn.test/meal.webp",
            AiRecognitionSource.Text,
            MealAiSessionStatus.Completed,
            new DateTime(2026, 3, 26, 12, 5, 0, DateTimeKind.Utc),
            "recognized",
            [CreateAiItemReadModel(sessionId, aiItemId)]);

    private static MealAiItemProjectionReadModel CreateAiItemReadModel(Guid sessionId, Guid aiItemId) =>
        new(
            aiItemId,
            sessionId,
            "Soup",
            NameLocal: "Soup local",
            Amount: 250,
            Unit: "g",
            Calories: 120,
            Proteins: 8,
            Fats: 3,
            Carbs: 16,
            Fiber: 2,
            Alcohol: 0,
            Confidence: 0.91,
            MealAiItemResolution.Candidate);

    private static CreateMealCommand CreateMealCommand(
        Guid? userId,
        Guid? imageAssetId = null,
        IReadOnlyList<MealItemInput>? items = null,
        IReadOnlyList<MealAiSessionInput>? aiSessions = null) =>
        new(
            userId,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Created",
            ImageUrl: null,
            imageAssetId,
            items ?? [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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

    private static UpdateMealCommand UpdateMealCommand(
        Guid? userId,
        Guid mealId,
        Guid? imageAssetId = null,
        IReadOnlyList<MealItemInput>? items = null,
        IReadOnlyList<MealAiSessionInput>? aiSessions = null,
        bool isNutritionAutoCalculated = true,
        double? manualCalories = 600,
        int preMealSatietyLevel = 3) =>
        new(
            userId,
            mealId,
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            MealType.Dinner.ToString(),
            "Updated",
            ImageUrl: null,
            imageAssetId,
            items ?? [new MealItemInput(ProductId.New().Value, RecipeId: null, 150)],
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

    private static MealAiSessionInput ValidAiSession(
        Guid? imageAssetId = null,
        string? notes = null,
        IReadOnlyList<MealAiItemInput>? items = null) =>
        new(
            imageAssetId,
            "Text",
            new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc),
            notes,
            items ?? [new MealAiItemInput("Soup", NameLocal: null, 250, "g", 120, 8, 3, 16, 2, 0)]);

    private static UpdateMealCommandHandler CreateUpdateHandler(
        IMealRepository repository,
        User user,
        RecordingRecentItemRepository? recentItems = null,
        IImageAssetAccessService? imageAccess = null) =>
        UpdateMealHandler(
            repository,
            new NoopMealNutritionService(),
            recentItems ?? new RecordingRecentItemRepository(),
            new RecordingCleanupService(),
            CreateCurrentUserAccessService(user),
            new StubDateTimeProvider(),
            imageAccess ?? FoodDiary.Application.Tests.Support.AllowImageAssetAccessService.Instance);

    private static MealProjectionReadModel ToMealProjectionReadModel(Meal meal) =>
        new(
            meal.Id.Value,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            meal.TotalAlcohol,
            meal.IsNutritionAutoCalculated,
            meal.ManualCalories,
            meal.ManualProteins,
            meal.ManualFats,
            meal.ManualCarbs,
            meal.ManualFiber,
            meal.ManualAlcohol,
            meal.PreMealSatietyLevel,
            meal.PostMealSatietyLevel,
            [.. meal.Items.Select(ToMealItemProjectionReadModel)],
            [.. meal.AiSessions.Select(ToMealAiSessionProjectionReadModel)]);

    private static MealItemProjectionReadModel ToMealItemProjectionReadModel(MealItem item) {
        bool hasSnapshot = item.HasNutritionSnapshot;
        return new MealItemProjectionReadModel(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.SnapshotName ?? item.Product?.Name,
            item.SnapshotImageUrl ?? item.Product?.ImageUrl,
            item.SnapshotUnit ?? item.Product?.BaseUnit.ToString(),
            item.SnapshotBaseAmount ?? item.Product?.BaseAmount,
            item.SnapshotCaloriesPerBase ?? item.Product?.CaloriesPerBase,
            item.SnapshotProteinsPerBase ?? item.Product?.ProteinsPerBase,
            item.SnapshotFatsPerBase ?? item.Product?.FatsPerBase,
            item.SnapshotCarbsPerBase ?? item.Product?.CarbsPerBase,
            item.SnapshotFiberPerBase ?? item.Product?.FiberPerBase,
            item.SnapshotAlcoholPerBase ?? item.Product?.AlcoholPerBase,
            item.Product?.ProductType,
            item.RecipeId?.Value,
            item.SnapshotName ?? item.Recipe?.Name,
            item.SnapshotImageUrl ?? item.Recipe?.ImageUrl,
            hasSnapshot ? 1 : item.Recipe?.Servings,
            item.SnapshotCaloriesPerBase ?? item.Recipe?.TotalCalories,
            item.SnapshotProteinsPerBase ?? item.Recipe?.TotalProteins,
            item.SnapshotFatsPerBase ?? item.Recipe?.TotalFats,
            item.SnapshotCarbsPerBase ?? item.Recipe?.TotalCarbs,
            item.SnapshotFiberPerBase ?? item.Recipe?.TotalFiber,
            item.SnapshotAlcoholPerBase ?? item.Recipe?.TotalAlcohol,
            item.SourceAiItemId?.Value,
            item.Origin);
    }

    private static MealAiSessionProjectionReadModel ToMealAiSessionProjectionReadModel(MealAiSession session) =>
        new(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            session.ImageAsset?.Url,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.Select(ToMealAiItemProjectionReadModel)]);

    private static MealAiItemProjectionReadModel ToMealAiItemProjectionReadModel(MealAiItem item) =>
        new(
            item.Id.Value,
            item.MealAiSessionId.Value,
            item.NameEn,
            item.NameLocal,
            item.Amount,
            item.Unit,
            item.Calories,
            item.Proteins,
            item.Fats,
            item.Carbs,
            item.Fiber,
            item.Alcohol,
            item.Confidence,
            item.Resolution);

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

        public Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
            MealId id,
            UserId userId,
            CancellationToken cancellationToken = default) {
            Meal? found = FindById(id, userId);
            return Task.FromResult(found is null ? null : ToMealProjectionReadModel(found));
        }

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

        public Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
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

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetCountAsync(
            UserId userId,
            MealQueryFilters filters,
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

        public Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
            UserId userId,
            DateTime date,
            int limit,
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

        public Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
            MealId id,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MealProjectionReadModel?>(_reloadStarted ? null : ToMealProjectionReadModel(meal));

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
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

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetCountAsync(
            UserId userId,
            MealQueryFilters filters,
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

        public Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
            UserId userId,
            DateTime date,
            int limit,
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

        public Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
            MealId id,
            UserId userId,
            CancellationToken cancellationToken = default) {
            Meal? found = StoredMeal is not null && StoredMeal.Id == id && StoredMeal.UserId == userId ? StoredMeal : null;
            return Task.FromResult(found is null ? null : ToMealProjectionReadModel(found));
        }

        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
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

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetCountAsync(
            UserId userId,
            MealQueryFilters filters,
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

        public Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
            UserId userId,
            DateTime date,
            int limit,
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

        public Task<MealProjectionReadModel?> GetByIdMealProjectionAsync(
            MealId id,
            UserId userId,
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

        public Task<(IReadOnlyList<MealProjectionReadModel> Items, int TotalItems)> GetPagedMealProjectionsAsync(
            UserId userId,
            int page,
            int limit,
            MealQueryFilters filters,
            CancellationToken cancellationToken = default) {
            LastDateFrom = filters.DateFrom;
            LastDateTo = filters.DateTo;
            LastMealTypes = filters.MealTypes;
            IReadOnlyList<MealProjectionReadModel> models = [.. _items.Select(ToMealProjectionReadModel)];
            return Task.FromResult((models, totalItems));
        }

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MealProjectionReadModel>> GetByPeriodMealProjectionsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> GetCountAsync(
            UserId userId,
            MealQueryFilters filters,
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

        public Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
            UserId userId,
            DateTime date,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>>(new Dictionary<ProductId, ProductOverviewReadItem>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>>(new Dictionary<RecipeId, RecipeOverviewReadItem>());
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
                Errors.Meal.InvalidData("Nutrition calculation failed.")));
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

    private static IMealReadService CreateMealReadService(
        IMealProjectionReadRepository mealRepository,
        IFavoriteMealRepository? favoriteMealRepository = null) {
        IFavoriteMealRepository repository = favoriteMealRepository ?? new StubFavoriteMealRepository();
        return new MealReadService(mealRepository, new FavoriteMealReadService(repository));
    }

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
        public Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteMeal favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteMeal>>(favorites);

        public Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteMealReadModel>>([.. favorites.Select(ToReadModel)]);

        public Task<FavoriteMeal?> GetByIdAsync(FavoriteMealId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<FavoriteMeal?>(null);

        public Task<FavoriteMeal?> GetByMealIdAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(favorites.FirstOrDefault(x => x.MealId == mealId));

        public Task<bool> ExistsByMealIdAsync(MealId mealId, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(favorites.Any(x => x.MealId == mealId));

        public Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
            UserId userId,
            IReadOnlyCollection<MealId> mealIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<MealId, FavoriteMeal>>(favorites.Where(x => mealIds.Contains(x.MealId)).ToDictionary(x => x.MealId));

        public Task<IReadOnlyDictionary<MealId, FavoriteMealId>> GetFavoriteIdsByMealIdsAsync(
            UserId userId,
            IReadOnlyCollection<MealId> mealIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<MealId, FavoriteMealId>>(
                favorites
                    .Where(x => mealIds.Contains(x.MealId))
                    .ToDictionary(x => x.MealId, x => x.Id));

        private static FavoriteMealReadModel ToReadModel(FavoriteMeal favorite) =>
            new(
                favorite.Id.Value,
                favorite.MealId.Value,
                favorite.Name,
                favorite.CreatedAtUtc,
                favorite.Meal.Date,
                favorite.Meal.MealType?.ToString(),
                favorite.Meal.TotalCalories,
                favorite.Meal.TotalProteins,
                favorite.Meal.TotalFats,
                favorite.Meal.TotalCarbs,
                favorite.Meal.Items.Count);
    }

    private static void SetFavoriteMealNavigation(FavoriteMeal favorite, Meal meal) {
        typeof(FavoriteMeal)
            .GetProperty(nameof(FavoriteMeal.Meal))!
            .SetValue(favorite, meal);
    }
}
