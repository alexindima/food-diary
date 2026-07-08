using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Consumptions.Models;
using FluentValidation.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Consumptions;

public partial class ConsumptionsFeatureTests {

    [Fact]
    public async Task GetConsumptionsOverviewQueryValidator_WithNullUserId_HasInvalidTokenError() {
        var validator = new GetConsumptionsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetConsumptionsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }


    [Fact]
    public async Task GetConsumptionsOverviewQueryValidator_WithValidUserId_HasNoErrors() {
        var validator = new GetConsumptionsOverviewQueryValidator();

        ValidationResult result = await validator.ValidateAsync(new GetConsumptionsOverviewQuery(Guid.NewGuid(), 1, 10, DateFrom: null, DateTo: null));

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch);

        var handler = new GetConsumptionByIdQueryHandler(
            CreateConsumptionReadService(new SingleMealRepository(meal)),
            CreateCurrentUserAccessService(User.Create("consumption-empty-id@example.com", "hash")));

        Result<ConsumptionModel> result = await handler.Handle(
            new GetConsumptionByIdQuery(userId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task GetConsumptionByIdQueryHandler_WithExistingConsumption_ReturnsMealModel() {
        var userId = UserId.New();
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            comment: "Owner note");
        meal.AddProduct(ProductId.New(), 150);
        meal.ApplyNutrition(new MealNutritionUpdate(350, 20, 12, 30, 4, 0, IsAutoCalculated: true));

        var handler = new GetConsumptionByIdQueryHandler(
            CreateConsumptionReadService(new SingleMealRepository(meal)),
            CreateCurrentUserAccessService(User.Create("consumption-existing@example.com", "hash")));

        Result<ConsumptionModel> result = await handler.Handle(new GetConsumptionByIdQuery(userId.Value, meal.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(meal.Id.Value, result.Value.Id);
        Assert.Equal("Owner note", result.Value.Comment);
        Assert.Single(result.Value.Items);
    }


    [Fact]
    public async Task GetConsumptionsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetConsumptionsQueryHandler_PreservesDateRangeInstantsForRepositoryQuery() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        var userId = UserId.New();
        var from = new DateTime(2026, 4, 4, 20, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 5, 19, 59, 59, 999, DateTimeKind.Utc);

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 25, from, to),
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
    public async Task GetConsumptionsQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }


    [Fact]
    public async Task GetConsumptionsQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(
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
    public async Task GetConsumptionsQueryHandler_WithMeals_ReturnsMappedFavoriteFlags() {
        var user = User.Create("paged-consumptions@example.com", "hash");
        var lunch = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        lunch.ApplyNutrition(new MealNutritionUpdate(420, 24, 14, 44, 5, 0, IsAutoCalculated: true));
        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(610, 38, 20, 58, 7, 0, IsAutoCalculated: true));
        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Evening favorite");
        SetFavoriteMealNavigation(favorite, dinner);
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(
                new RecordingMealPageRepository([lunch, dinner], totalItems: 2),
                new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Data.Count);
        Assert.False(result.Value.Data.Single(item => item.Id == lunch.Id.Value).IsFavorite);
        ConsumptionModel favoriteMeal = result.Value.Data.Single(item => item.Id == dinner.Id.Value);
        Assert.True(favoriteMeal.IsFavorite);
        Assert.Equal(favorite.Id.Value, favoriteMeal.FavoriteMealId);
    }


    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_ReturnsFavoritePreviewAndFavoriteFlags() {
        var user = User.Create("overview-consumptions@example.com", "hash");
        var breakfast = Meal.Create(user.Id, new DateTime(2026, 3, 26, 8, 0, 0, DateTimeKind.Utc), MealType.Breakfast);
        breakfast.ApplyNutrition(new MealNutritionUpdate(250, 12, 8, 24, 3, 0, IsAutoCalculated: true));

        var dinner = Meal.Create(user.Id, new DateTime(2026, 3, 26, 18, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinner.ApplyNutrition(new MealNutritionUpdate(640, 40, 24, 52, 6, 0, IsAutoCalculated: true));

        var favorite = FavoriteMeal.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteMealNavigation(favorite, dinner);

        var repository = new RecordingMealPageRepository([breakfast, dinner], totalItems: 2);
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository, new StubFavoriteMealRepository([favorite])),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.AllConsumptions.Data.Count);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.True(result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.AllConsumptions.Data.Single(x => x.Id == dinner.Id.Value).FavoriteMealId);
    }


    [Theory]
    [InlineData(null)]
    [InlineData("not-a-meal")]
    public async Task GetConsumptionsOverviewQueryHandler_WithEmptyOrInvalidMealTypes_PassesNullMealTypeFilter(string? mealType) {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-filter@example.com", "hash")));
        var userId = UserId.New();
        IReadOnlyCollection<string>? mealTypes = mealType is null ? null : [mealType];

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(userId.Value, 1, 10, DateFrom: null, DateTo: null, MealTypes: mealTypes),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(repository.LastMealTypes);
    }


    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithDuplicateValidMealTypes_DistinctsMealTypeFilter() {
        var repository = new RecordingMealPageRepository();
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(repository),
            CreateCurrentUserAccessService(User.Create("overview-meal-type-distinct@example.com", "hash")));
        var userId = UserId.New();

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(
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
    public async Task GetConsumptionByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionByIdQueryHandler(
            CreateConsumptionReadService(new CreatingMealRepository()),
            Substitute.For<ICurrentUserAccessService>());

        Result<ConsumptionModel> result = await handler.Handle(
            new GetConsumptionByIdQuery(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(UserId: null, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetConsumptionsOverviewQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-overview-consumptions@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetConsumptionsOverviewQueryHandler(
            CreateConsumptionReadService(new RecordingMealPageRepository()),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionOverviewModel> result = await handler.Handle(
            new GetConsumptionsOverviewQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task GetConsumptionsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-consumption@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetConsumptionsQueryHandler(
            CreateConsumptionReadService(new CreatingMealRepository()),
            CreateCurrentUserAccessService(user));

        Result<PagedResponse<ConsumptionModel>> result = await handler.Handle(
            new GetConsumptionsQuery(user.Id.Value, 1, 10, DateFrom: null, DateTo: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

}
