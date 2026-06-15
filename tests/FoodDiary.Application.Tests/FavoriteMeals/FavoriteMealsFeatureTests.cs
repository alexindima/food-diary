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
            CreateFavoriteMealRepository(), CreateMealRepository(meal: null), CreateUserRepository(user: null));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(UserId: null, Guid.NewGuid(), Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenUserNotFound_ReturnsFailure() {
        var handler = new AddFavoriteMealCommandHandler(
            CreateFavoriteMealRepository(), CreateMealRepository(meal: null), CreateUserRepository(user: null));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid(), Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddFavoriteMeal_WhenMealNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new AddFavoriteMealCommandHandler(
            CreateFavoriteMealRepository(), CreateMealRepository(meal: null), CreateUserRepository(user));

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
        IFavoriteMealRepository favRepo = CreateFavoriteMealRepository(existingByMealId: existing);

        var handler = new AddFavoriteMealCommandHandler(
            favRepo, CreateMealRepository(meal), CreateUserRepository(user));

        Result<FavoriteMealModel> result = await handler.Handle(
            new AddFavoriteMealCommand(user.Id.Value, meal.Id.Value, Name: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyExists", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WithNullUserId_ReturnsFailure() {
        var handler = new RemoveFavoriteMealCommandHandler(
            CreateFavoriteMealRepository(), CreateUserRepository(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenNotFound_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new RemoveFavoriteMealCommandHandler(
            CreateFavoriteMealRepository(), CreateUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenUserNotFound_ReturnsFailure() {
        var handler = new RemoveFavoriteMealCommandHandler(
            CreateFavoriteMealRepository(),
            CreateUserRepository(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RemoveFavoriteMeal_WhenExists_Succeeds() {
        var user = User.Create("user@example.com", "hash");
        var favorite = FavoriteMeal.Create(user.Id, MealId.New());
        IFavoriteMealRepository favRepo = CreateFavoriteMealRepository(existingById: favorite);

        var handler = new RemoveFavoriteMealCommandHandler(favRepo, CreateUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteMealCommand(user.Id.Value, favorite.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await favRepo.Received(1).DeleteAsync(favorite, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsMealFavorite_WhenFavoriteExists_ReturnsTrue() {
        var user = User.Create("user@example.com", "hash");
        var meal = Meal.Create(user.Id, DateTime.UtcNow, MealType.Dinner);
        var favorite = FavoriteMeal.Create(user.Id, meal.Id);
        var handler = new IsMealFavoriteQueryHandler(
            CreateFavoriteMealRepository(existingByMealId: favorite),
            CreateUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(user.Id.Value, meal.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsMealFavorite_WhenFavoriteIsMissing_ReturnsFalse() {
        var user = User.Create("user@example.com", "hash");
        var handler = new IsMealFavoriteQueryHandler(
            CreateFavoriteMealRepository(),
            CreateUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsMealFavorite_WhenUserNotFound_ReturnsFailure() {
        var handler = new IsMealFavoriteQueryHandler(
            CreateFavoriteMealRepository(),
            CreateUserRepository(user: null));

        Result<bool> result = await handler.Handle(
            new IsMealFavoriteQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task IsMealFavorite_WithNullUserId_ReturnsFailure() {
        var handler = new IsMealFavoriteQueryHandler(
            CreateFavoriteMealRepository(),
            CreateUserRepository(user: null));

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
            CreateFavoriteMealRepository(favorites: [favorite]),
            CreateUserRepository(user));

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
            CreateFavoriteMealRepository(),
            CreateUserRepository(user: null));

        Result<IReadOnlyList<FavoriteMealModel>> result = await handler.Handle(new GetFavoriteMealsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetFavoriteMeals_WithNullUserId_ReturnsFailure() {
        var handler = new GetFavoriteMealsQueryHandler(
            CreateFavoriteMealRepository(),
            CreateUserRepository(user: null));

        Result<IReadOnlyList<FavoriteMealModel>> result = await handler.Handle(new GetFavoriteMealsQuery(UserId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private static IFavoriteMealRepository CreateFavoriteMealRepository(
        FavoriteMeal? existingByMealId = null,
        FavoriteMeal? existingById = null,
        IReadOnlyList<FavoriteMeal>? favorites = null) {
        IFavoriteMealRepository repository = Substitute.For<IFavoriteMealRepository>();
        repository
            .GetByMealIdAsync(Arg.Any<MealId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(existingByMealId));
        repository
            .GetByIdAsync(Arg.Any<FavoriteMealId>(), Arg.Any<UserId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(existingById));
        repository
            .GetByMealIdsAsync(Arg.Any<UserId>(), Arg.Any<IReadOnlyCollection<MealId>>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                IReadOnlyDictionary<MealId, FavoriteMeal> result = existingByMealId is null
                    ? []
                    : new Dictionary<MealId, FavoriteMeal> { [existingByMealId.MealId] = existingByMealId };

                return Task.FromResult(result);
            });
        repository
            .AddAsync(Arg.Any<FavoriteMeal>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<FavoriteMeal>()));
        repository
            .DeleteAsync(Arg.Any<FavoriteMeal>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        repository
            .GetAllAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(favorites ?? []));
        return repository;
    }

    private static IMealRepository CreateMealRepository(Meal? meal) {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetByIdAsync(Arg.Any<MealId>(), Arg.Any<UserId>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(meal));
        return repository;
    }

    private static IUserRepository CreateUserRepository(User? user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(user));
        return repository;
    }

    private static void SetFavoriteMealNavigation(FavoriteMeal favorite, Meal meal) {
        typeof(FavoriteMeal)
            .GetProperty(nameof(FavoriteMeal.Meal))!
            .SetValue(favorite, meal);
    }
}
