using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;
using FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.FavoriteMeals;

public class FavoriteMealsFeatureTests {
    [Fact]
    public async Task AddFavoriteMeal_WithNullUserId_ReturnsFailure() {
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(null), new StubUserRepository(null));

        var result = await handler.Handle(
            new AddFavoriteMealCommand(null, Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenUserNotFound_ReturnsFailure() {
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(null), new StubUserRepository(null));

        var result = await handler.Handle(
            new AddFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenMealNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AddFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubMealRepository(null), new StubUserRepository(user));

        var result = await handler.Handle(
            new AddFavoriteMealCommand(user.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenAlreadyExists_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.UtcNow, MealType.Lunch);
        var existing = FavoriteMeal.Create(user.Id, meal.Id);
        var favRepo = new StubFavoriteMealRepository(existingByMealId: existing);

        var handler = new AddFavoriteMealCommandHandler(
            favRepo, new StubMealRepository(meal), new StubUserRepository(user));

        var result = await handler.Handle(
            new AddFavoriteMealCommand(user.Id.Value, meal.Id.Value, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WithNullUserId_ReturnsFailure() {
        var handler = new RemoveFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubUserRepository(null));

        var result = await handler.Handle(
            new RemoveFavoriteMealCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RemoveFavoriteMealCommandHandler(
            new StubFavoriteMealRepository(), new StubUserRepository(user));

        var result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenExists_Succeeds() {
        var user = User.Create("user@example.com", "hash");
        var favorite = FavoriteMeal.Create(user.Id, MealId.New());
        var favRepo = new StubFavoriteMealRepository(existingById: favorite);

        var handler = new RemoveFavoriteMealCommandHandler(favRepo, new StubUserRepository(user));

        var result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, favorite.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(favRepo.DeleteCalled);
    }

    private sealed class StubFavoriteMealRepository(
        FavoriteMeal? existingByMealId = null,
        FavoriteMeal? existingById = null) : IFavoriteMealRepository {
        public bool DeleteCalled { get; private set; }

        public Task<FavoriteMeal?> GetByMealIdAsync(MealId mealId, UserId userId, CancellationToken ct = default) =>
            Task.FromResult(existingByMealId);

        public Task<FavoriteMeal?> GetByIdAsync(FavoriteMealId id, UserId userId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(existingById);

        public Task<IReadOnlyDictionary<MealId, FavoriteMeal>> GetByMealIdsAsync(
            UserId userId,
            IReadOnlyCollection<MealId> mealIds,
            CancellationToken ct = default) {
            IReadOnlyDictionary<MealId, FavoriteMeal> result = existingByMealId is null
                ? new Dictionary<MealId, FavoriteMeal>()
                : new Dictionary<MealId, FavoriteMeal> { [existingByMealId.MealId] = existingByMealId };

            return Task.FromResult(result);
        }

        public Task<FavoriteMeal> AddAsync(FavoriteMeal favorite, CancellationToken ct = default) =>
            Task.FromResult(favorite);

        public Task DeleteAsync(FavoriteMeal favorite, CancellationToken ct = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FavoriteMeal>> GetAllAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FavoriteMeal>>([]);
    }

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
}
