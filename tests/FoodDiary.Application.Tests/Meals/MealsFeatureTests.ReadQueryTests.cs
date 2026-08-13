using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Queries.GetMealById;
using FoodDiary.Application.Meals.Queries.GetMeals;
using FoodDiary.Application.Meals.Queries.GetMealsOverview;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Meals.Models;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Tests.Meals;

public partial class MealsFeatureTests {

    [Fact]
    public async Task GetMealsOverviewQueryValidator_WithNullUserId_HasInvalidTokenError() {
        var validator = new GetMealsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetMealsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetMealsOverviewQueryValidator_WithValidUserId_HasNoErrors() {
        var validator = new GetMealsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetMealsOverviewQuery(Guid.NewGuid(), 1, 10, DateFrom: null, DateTo: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetMealByIdQueryHandler_WithEmptyMealId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        var handler = new GetMealByIdQueryHandler(
            CreateMealReadService(new SingleMealRepository(meal)),
            CreateCurrentUserAccessService(User.Create("meal-empty-id@example.com", "hash")));

        Result<MealModel> result = await handler.Handle(
            new GetMealByIdQuery(userId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("MealId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMealByIdQueryHandler_WithExistingMeal_ReturnsMealModel() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            comment: "Owner note");
        meal.AddProduct(ProductId.New(), 150);
        meal.ApplyNutrition(new MealNutritionUpdate(350, 20, 12, 30, 4, 0, IsAutoCalculated: true));

        var handler = new GetMealByIdQueryHandler(
            CreateMealReadService(new SingleMealRepository(meal)),
            CreateCurrentUserAccessService(User.Create("meal-existing@example.com", "hash")));

        Result<MealModel> result = await handler.Handle(new GetMealByIdQuery(userId.Value, meal.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(meal.Id.Value, result.Value.Id);
        Assert.Equal("Owner note", result.Value.Comment);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetMealsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetMealsQueryHandler_PreservesDateRangeInstantsForRepositoryQuery() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(repository),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        var userId = UserId.New();
        var from = new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 5, 19, 59, 59, 999, DateTimeKind.Utc);

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(userId.Value, 1, 25, from, to),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(from, repository.LastDateFrom);
        Assert.Equal(to, repository.LastDateTo);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateFrom!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, repository.LastDateTo!.Value.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-meal")]
    public async Task GetMealsQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }

    [Fact]
    public async Task GetMealsQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(
                userId.Value,
                1,
                10,
                DateFrom: null,
                DateTo: null,
                MealTypes: ["Lunch", "lunch", "Dinner", "unknown"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([MealType.Lunch, MealType.Dinner], repository.LastMealTypes);
    }

    [Fact]
    public async Task GetMealsQueryHandler_WithMeals_ReturnsMappedFavoriteFlags() {
        var user = User.Create("paged-meals@example.com", "hash");
        var lunch = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        lunch.ApplyNutrition(new MealNutritionUpdate(420, 24, 14, 44, 5, 0, IsAutoCalculated: true));
        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(610, 38, 20, 58, 7, 0, IsAutoCalculated: true));
        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Evening favorite");
        SetFavoriteMealNavigation(favorite, dinner);
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(
                new RecordingMealPageRepository([lunch, dinner], totalItems: 2),
                new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Data.Count);
        Assert.False(result.Value.Data.Single(item => item.Id == lunch.Id.Value).IsFavorite);
        MealModel favoriteMeal = result.Value.Data.Single(item => item.Id == dinner.Id.Value);
        Assert.True(favoriteMeal.IsFavorite);
        Assert.Equal(favorite.Id.Value, favoriteMeal.FavoriteMealId);
    }

    [Fact]
    public async Task GetMealsOverviewQueryHandler_ReturnsFavoritePreviewAndFavoriteFlags() {
        var user = User.Create("overview-meals@example.com", "hash");
        var breakfast = Meal.Create(user.Id, new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc), MealType.Breakfast);
        breakfast.ApplyNutrition(new MealNutritionUpdate(250, 12, 8, 24, 3, 0, IsAutoCalculated: true));

        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(640, 40, 24, 52, 6, 0, IsAutoCalculated: true));

        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteMealNavigation(favorite, dinner);

        var repository = new RecordingMealPageRepository([breakfast, dinner], totalItems: 2);
        var handler = new GetMealsOverviewQueryHandler(
            CreateMealReadService(repository, new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<MealOverviewModel> result = await handler.Handle(
            new GetMealsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.AllMeals.Data.Count);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.True(result.Value.AllMeals.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.AllMeals.Data.Single(x => x.Id == dinner.Id.Value).FavoriteMealId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-meal")]
    public async Task GetMealsOverviewQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetMealsOverviewQueryHandler(
            CreateMealReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<MealOverviewModel> result = await handler.Handle(
            new GetMealsOverviewQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }

    [Fact]
    public async Task GetMealsOverviewQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetMealsOverviewQueryHandler(
            CreateMealReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<MealOverviewModel> result = await handler.Handle(
            new GetMealsOverviewQuery(
                userId.Value,
                1,
                10,
                DateFrom: null,
                DateTo: null,
                MealTypes: ["Breakfast", "breakfast", "Snack", "unknown"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal([MealType.Breakfast, MealType.Snack], repository.LastMealTypes);
    }

    [Fact]
    public async Task GetMealByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetMealByIdQueryHandler(
            CreateMealReadService(new CreatingMealRepository()),
            Substitute.For<ICurrentUserAccessService>());

        Result<MealModel> result = await handler.Handle(
            new GetMealByIdQuery(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetMealsOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetMealsOverviewQueryHandler(
            CreateMealReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<MealOverviewModel> result = await handler.Handle(
            new GetMealsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetMealsOverviewQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-overview-meals@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetMealsOverviewQueryHandler(
            CreateMealReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(user));

        Result<MealOverviewModel> result = await handler.Handle(
            new GetMealsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetMealsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-meal@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetMealsQueryHandler(
            CreateMealReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<MealModel>> result = await handler.Handle(
            new GetMealsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

}
