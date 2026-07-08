using FoodDiary.Results;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Tests.Consumptions;

public partial class ConsumptionsFeatureTests {

    [Fact]
    public async Task DeleteConsumptionCommandHandler_WithEmptyConsumptionId_ReturnsValidationFailure() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ConsumptionId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DeleteConsumptionCommandHandler_WhenMealIsMissing_ReturnsNotFound() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.NotFound", result.Error.Code);
    }


    [Fact]
    public async Task DeleteConsumptionCommandHandler_WhenMealExists_DeletesMeal() {
        var user = User.Create("delete-consumption@example.com", "hash");
        var meal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new SingleMealRepository(meal);
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(repository);

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(user.Id.Value, meal.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(meal, repository.DeletedMeal);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithExistingMeal_CopiesItemsAndAppliesNutrition() {
        var user = User.Create("repeat-meal@example.com", "hash");
        var sourceMeal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        sourceMeal.AddProduct(ProductId.New(), 200);
        sourceMeal.AddRecipe(RecipeId.New(), 1);

        var repository = new SingleMealRepository(sourceMeal);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(510, 33, 18, 47, 5, 0)),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedMeal);
        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), repository.LastAddedMeal.Date);
        Assert.Equal(MealType.Dinner, repository.LastAddedMeal.MealType);
        Assert.Equal(2, repository.LastAddedMeal.Items.Count);
        Assert.Equal(510, repository.LastAddedMeal.TotalCalories);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithAiAndManualNutrition_CopiesFullConsumption() {
        var user = User.Create("repeat-ai-meal@example.com", "hash");
        var staleMealImageAssetId = ImageAssetId.New();
        var aiImageAssetId = ImageAssetId.New();
        var sourceMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 26, 20, 0, 0, DateTimeKind.Utc),
            MealType.Dinner,
            imageUrl: "https://example.com/stale-meal-cover.jpg",
            imageAssetId: staleMealImageAssetId);
        sourceMeal.AddAiSession(
            imageAssetId: aiImageAssetId,
            source: AiRecognitionSource.Photo,
            recognizedAtUtc: new DateTime(2026, 3, 26, 20, 1, 0, DateTimeKind.Utc),
            notes: "photo",
            items: [
                MealAiItemData.Create("Pasta", "Паста", 250, "g", 420, 14, 8, 72, 4, 0),
            ]);
        sourceMeal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 430,
            TotalProteins: 15,
            TotalFats: 9,
            TotalCarbs: 73,
            TotalFiber: 4,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: 430,
            ManualProteins: 15,
            ManualFats: 9,
            ManualCarbs: 73,
            ManualFiber: 4,
            ManualAlcohol: 0));

        var repository = new SingleMealRepository(sourceMeal);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            repository,
            new FixedMealNutritionService(new MealNutritionSummary(0, 0, 0, 0, 0, 0)),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, new DateTime(2026, 3, 27, 19, 30, 0, DateTimeKind.Utc), MealType.Dinner.ToString()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedMeal);
        Assert.Single(repository.LastAddedMeal.AiSessions);
        Assert.Single(repository.LastAddedMeal.AiSessions.Single().Items);
        Assert.Equal("Pasta", repository.LastAddedMeal.AiSessions.Single().Items.Single().NameEn);
        Assert.Equal(aiImageAssetId, repository.LastAddedMeal.AiSessions.Single().ImageAssetId);
        Assert.Null(repository.LastAddedMeal.ImageUrl);
        Assert.Null(repository.LastAddedMeal.ImageAssetId);
        Assert.False(repository.LastAddedMeal.IsNutritionAutoCalculated);
        Assert.Equal(430, repository.LastAddedMeal.TotalCalories);
        Assert.Equal(430, repository.LastAddedMeal.ManualCalories);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(UserId: null, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-repeat-meal@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithEmptyMealId_ReturnsValidationFailure() {
        var user = User.Create("repeat-empty-meal-id@example.com", "hash");
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, Guid.Empty, DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("MealId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WhenSourceMealMissing_ReturnsNotFound() {
        var user = User.Create("repeat-missing-source@example.com", "hash");
        RepeatMealCommandHandler handler = RepeatMealHandler(
            new CreatingMealRepository(),
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, Guid.NewGuid(), DateTime.UtcNow, MealType.Lunch.ToString()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Consumption.NotFound", result.Error.Code);
    }


    [Fact]
    public async Task RepeatMealCommandHandler_WithInvalidMealType_ReturnsValidationFailure() {
        var user = User.Create("repeat-invalid-meal-type@example.com", "hash");
        var sourceMeal = Meal.Create(user.Id, new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        var repository = new CreatingMealRepository();
        await repository.AddAsync(sourceMeal);
        RepeatMealCommandHandler handler = RepeatMealHandler(
            repository,
            new NoopMealNutritionService(),
            CreateCurrentUserAccessService(user));

        Result<ConsumptionModel> result = await handler.Handle(
            new RepeatMealCommand(user.Id.Value, sourceMeal.Id.Value, DateTime.UtcNow, "Brunch"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("MealType", result.Error.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task DeleteConsumptionCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        DeleteConsumptionCommandHandler handler = DeleteConsumptionHandler(new CreatingMealRepository());

        Result result = await handler.Handle(
            new DeleteConsumptionCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

}
