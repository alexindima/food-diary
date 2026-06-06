using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;
using FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteMeals.Models;

namespace FoodDiary.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public class FavoriteMealsFeatureTests {
    [Fact]
    public async Task AddFavoriteMeal_WithNullUserId_ReturnsFailure() {
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(meal: null), new StubUserRepository(user: null));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(UserId: null, Guid.NewGuid(), Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenUserNotFound_ReturnsFailure() {
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(meal: null), new StubUserRepository(user: null));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid(), Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenMealNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(meal: null), new StubUserRepository(user));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(user.Id.Value, Guid.NewGuid(), Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenAlreadyExists_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.UtcNow, MealType.Lunch);
        var existing = FavoriteMeal.Create(user.Id, meal.Id);
        var favRepo = new StubFavoriteMealRepository(existingByMealId: existing);

        var handler = new AddFavoriteMealCommandHandler(
            favRepo, new StubMealRepository(meal), new StubUserRepository(user));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(user.Id.Value, meal.Id.Value, Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyExists", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WithNullUserId_ReturnsFailure() {
        var handler = new RemoveFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubUserRepository(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RemoveFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenUserNotFound_ReturnsFailure() {
        var handler = new RemoveFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenExists_Succeeds() {
        var user = User.Create("user@example.com", "hash");
        var favorite = FavoriteMeal.Create(user.Id, MealId.New());
        var favRepo = new StubFavoriteMealRepository(existingById: favorite);

        var handler = new RemoveFavoriteMealCommandHandler(favRepo, new StubUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, favorite.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(favRepo.DeleteCalled);
    }

    [Fact]
    public async Task IsMealFavorite_WhenFavoriteExists_ReturnsTrue() {
        var user = User.Create("user@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.UtcNow, MealType.Dinner);
        var favorite = FavoriteMeal.Create(user.Id, meal.Id);
        var handler = new IsMealFavoriteQueryHandler(
            new StubFavoriteMealRepository(existingByMealId: favorite),
            new StubUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(user.Id.Value, meal.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsMealFavorite_WhenFavoriteIsMissing_ReturnsFalse() {
        var user = User.Create("user@example.com", "hash");
        var handler = new IsMealFavoriteQueryHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsMealFavorite_WhenUserNotFound_ReturnsFailure() {
        var handler = new IsMealFavoriteQueryHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user: null));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task IsMealFavorite_WithNullUserId_ReturnsFailure() {
        var handler = new IsMealFavoriteQueryHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user: null));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetFavoriteMeals_WhenUserCanAccess_ReturnsMappedMeals() {
        var user = User.Create("user@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 450,
            TotalProteins: 35,
            TotalFats: 12,
            TotalCarbs: 42,
            TotalFiber: 7,
            TotalAlcohol: 0,
            IsAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null));
        meal.AddProduct(ProductId.New(), 100);
        var favorite = FavoriteMeal.Create(user.Id, meal.Id, "  Work lunch  ");
        SetFavoriteMealNavigation(favorite, meal);
        var handler = new GetFavoriteMealsQueryHandler(
            new StubFavoriteMealRepository(favorites: [favorite]),
            new StubUserRepository(user));

        Result<IReadOnlyList<FavoriteMealModel>> result = await handler.Handle(new GetFavoriteMealsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        FavoriteMealModel model = Assert.Single(result.Value);
        Assert.Equal(favorite.Id.Value, model.Id);
        Assert.Equal(meal.Id.Value, model.MealId);
        Assert.Equal("Work lunch", model.Name);
        Assert.Equal(meal.Date, model.MealDate);
        Assert.Equal("Lunch", model.MealType);
        Assert.Equal(450, model.TotalCalories);
        Assert.Equal(35, model.TotalProteins);
        Assert.Equal(12, model.TotalFats);
        Assert.Equal(42, model.TotalCarbs);
        Assert.Equal(1, model.ItemCount);
    }

    [Fact]
    public async Task GetFavoriteMeals_WhenUserIsMissing_ReturnsFailure() {
        var handler = new GetFavoriteMealsQueryHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user: null));

        Result<IReadOnlyList<FavoriteMealModel>> result = await handler.Handle(new GetFavoriteMealsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetFavoriteMeals_WithNullUserId_ReturnsFailure() {
        var handler = new GetFavoriteMealsQueryHandler(
            new StubFavoriteMealRepository(),
            new StubUserRepository(user: null));

        Result<IReadOnlyList<FavoriteMealModel>> result = await handler.Handle(new GetFavoriteMealsQuery(UserId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFavoriteMealRepository(
        FavoriteMeal? existingByMealId = null,
        FavoriteMeal? existingById = null,
        IReadOnlyList<FavoriteMeal>? favorites = null) : IFavoriteMealRepository {
        private readonly FavoriteMeal? _existingByMealId = existingByMealId;
        private readonly FavoriteMeal? _existingById = existingById;
        private readonly IReadOnlyList<FavoriteMeal> _favorites = favorites ?? [];

        public bool DeleteCalled { get; private set; }

        public Task<FavoriteMeal?> GetByMealIdAsync(MealId mealId, UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_existingByMealId);

        public Task<FavoriteMeal?> GetByIdAsync(FavoriteMealId id, UserId userId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_existingById);

        public Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
            UserId userId,
            IReadOnlyCollection<MealId> mealIds,
            CancellationToken ct = default) {
            IReadOnlyDictionary<MealId, FavoriteMeal> result = _existingByMealId is null
                ? []
                : new Dictionary<MealId, FavoriteMeal> { [_existingByMealId.MealId] = _existingByMealId };

            return Task.FromResult(result);
        }

        public Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken ct = default) =>
            Task.FromResult(favorite);

        public Task DeleteAsync(FavoriteMeal favorite, CancellationToken ct = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_favorites);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubMealRepository(Meal? meal) : IMealRepository {
        public Task<Meal?> GetByIdAsync(MealId id, UserId userId, bool includeItems = false, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(meal);

        public Task<Meal> AddAsync(Meal m, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Meal m, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Meal m, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(UserId userId, int page, int limit, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult(user);

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static void SetFavoriteMealNavigation(FavoriteMeal favorite, Meal meal) {
        typeof(FavoriteMeal)
            .GetProperty(nameof(FavoriteMeal.Meal))!
            .SetValue(favorite, meal);
    }
}
