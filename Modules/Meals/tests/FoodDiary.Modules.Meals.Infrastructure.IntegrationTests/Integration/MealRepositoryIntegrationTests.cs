using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Meals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Projection_LoadsAiImageAndLegacyRecipeWithoutTrackingForeignEntities() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"projection-{Guid.NewGuid():N}@example.com", "hash");
        var image = FoodDiary.Domain.Entities.Assets.ImageAsset.Create(user.Id, "images/meal.jpg", "https://cdn.example.com/meal.jpg");
        var recipe = FoodDiary.Domain.Entities.Recipes.Recipe.Create(user.Id, "Legacy recipe", 2);
        var meal = Meal.Create(user.Id, DateTime.UtcNow);
        meal.AddRecipe(recipe.Id, 1);
        meal.AddAiSession(image.Id, AiRecognitionSource.Photo, DateTime.UtcNow, notes: null,
            [MealAiItemData.Create("Apple", nameLocal: null, 100, "g", 52, 0.3, 0.2, 14, 2.4, 0)]);
        context.AddRange(user, image, recipe, meal);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        MealProjectionReadModel? result = await new MealRepository(context).GetByIdMealProjectionAsync(meal.Id, user.Id);

        Assert.NotNull(result);
        Assert.Equal("Legacy recipe", Assert.Single(result.Items).RecipeName);
        Assert.Equal(image.Url, Assert.Single(result.AiSessions).ImageUrl);
        Assert.Empty(context.ChangeTracker.Entries());
    }
    [RequiresDockerFact]
    public async Task MealUser_ScalarForeignKey_RetainsSoftDeletedMealsAndCascadesOnPurge() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meal-owner-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"meal-survivor-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(otherUser.Id, "Shared apple", MeasurementUnit.G, 100, 100,
            52, 0.3, 0.2, 14, 2.4, 0);
        var meal = Meal.Create(user.Id, DateTime.UtcNow);
        MealItem item = meal.AddProduct(product.Id, 100);
        MealAiSession session = meal.AddAiSession(imageAssetId: null, AiRecognitionSource.Text, DateTime.UtcNow,
            notes: null, [MealAiItemData.Create("Apple", nameLocal: null, 100, "g", 52, 0.3, 0.2, 14, 2.4, 0)]);
        MealAiItem aiItem = Assert.Single(session.Items);
        var otherMeal = Meal.Create(otherUser.Id, DateTime.UtcNow);
        context.Users.AddRange(user, otherUser);
        context.Products.Add(product);
        context.Meals.AddRange(meal, otherMeal);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Meal loaded = await context.Meals.SingleAsync(entry => entry.Id == meal.Id);
        IForeignKey relationship = context.Model.FindEntityType(typeof(Meal))!.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(User));
        Assert.Multiple(
            () => Assert.Equal(user.Id, loaded.UserId),
            () => Assert.Null(relationship.PrincipalToDependent),
            () => Assert.Null(relationship.DependentToPrincipal),
            () => Assert.True(relationship.IsRequired),
            () => Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior),
            () => Assert.Equal("FK_Meals_Users_UserId", relationship.GetConstraintName()),
            () => Assert.Null(context.Model.FindEntityType(typeof(User))!.FindNavigation("Meals")));

        User loadedUser = await context.Users.SingleAsync(entry => entry.Id == loaded.UserId);
        loadedUser.MarkDeleted(DateTime.UtcNow);
        await context.SaveChangesAsync();
        Assert.True(await context.Meals.AnyAsync(entry => entry.Id == meal.Id));
        loadedUser.Restore();
        await context.SaveChangesAsync();
        Assert.True(await context.Meals.AnyAsync(entry => entry.Id == meal.Id));

        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.Users.Where(entry => entry.Id == user.Id).ExecuteDeleteAsync());
        Assert.False(await context.Meals.AnyAsync(entry => entry.Id == meal.Id));
        Assert.False(await context.Set<MealItem>().AnyAsync(entry => entry.Id == item.Id));
        Assert.False(await context.Set<MealAiSession>().AnyAsync(entry => entry.Id == session.Id));
        Assert.False(await context.Set<MealAiItem>().AnyAsync(entry => entry.Id == aiItem.Id));
        Assert.True(await context.Meals.AnyAsync(entry => entry.Id == otherMeal.Id));
        Assert.True(await context.Products.AnyAsync(entry => entry.Id == product.Id));
    }

    [RequiresDockerFact]
    public async Task GetByIdMealProjectionAsync_WithSnapshot_PreservesNutritionAndProductType() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"snapshot-type-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(user.Id, "Apple", MeasurementUnit.G, 100, 100,
            52, 0.3, 0.2, 14, 2.4, 0, productType: ProductType.Fruit);
        var meal = Meal.Create(user.Id, DateTime.UtcNow);
        MealItem item = meal.AddProduct(product.Id, 100);
        item.ApplyProductSnapshot("Original apple", imageUrl: null, MeasurementUnit.G, 100,
            40, 0.2, 0.1, 10, 2, 0);
        context.Users.Add(user);
        context.Products.Add(product);
        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);
        MealProjectionReadModel? projection = await repository.GetByIdMealProjectionAsync(meal.Id, user.Id);

        Assert.NotNull(projection);
        MealItemProjectionReadModel projected = Assert.Single(projection.Items);
        Assert.Equal(ProductType.Fruit, projected.ProductType);
        Assert.Equal(40, projected.ProductCaloriesPerBase);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesDateFilterAndKeepsPagingMetadata() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var olderMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        var newerMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc));
        var filteredOutMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(olderMeal, newerMeal, filteredOutMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 1,
            filters: new MealQueryFilters(
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)));

        Meal item = Assert.Single(items);
        Assert.Equal(2, totalItems);
        Assert.Equal(newerMeal.Id, item.Id);

        (IReadOnlyList<MealProjectionReadModel> readModels, int readModelTotalItems) = await repository.GetPagedMealProjectionsAsync(
            user.Id,
            page: 1,
            limit: 1,
            filters: new MealQueryFilters(
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc)));
        MealProjectionReadModel readModel = Assert.Single(readModels);
        Assert.Equal(2, readModelTotalItems);
        Assert.Equal(newerMeal.Id.Value, readModel.Id);

        MealProjectionReadModel? byIdReadModel = await repository.GetByIdMealProjectionAsync(newerMeal.Id, user.Id);
        Assert.NotNull(byIdReadModel);
        Assert.Equal(newerMeal.Id.Value, byIdReadModel.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_IncludesMealsThroughoutDateToDay() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-time-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var morningMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 8, 15, 0, DateTimeKind.Utc));
        var eveningMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 21, 30, 0, DateTimeKind.Utc));
        var nextDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(morningMeal, eveningMeal, nextDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            filters: new MealQueryFilters(
                new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(eveningMeal.Id, item.Id),
            item => Assert.Equal(morningMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_UsesExactUtcInstantsForLocalDayBoundaries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-local-day-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var previousLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 18, 48, 0, DateTimeKind.Utc));
        var firstLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 20, 30, 0, DateTimeKind.Utc));
        var lastLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 5, 19, 30, 0, DateTimeKind.Utc));
        var nextLocalDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 5, 20, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(previousLocalDayMeal, firstLocalDayMeal, lastLocalDayMeal, nextLocalDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        (IReadOnlyList<Meal>? items, int totalItems) = await repository.GetPagedAsync(
            user.Id,
            page: 1,
            limit: 10,
            filters: new MealQueryFilters(
                new DateTime(2026, 5, 4, 20, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 5, 19, 59, 59, 999, DateTimeKind.Utc)));

        Assert.Equal(2, totalItems);
        Assert.Collection(
            items,
            item => Assert.Equal(lastLocalDayMeal.Id, item.Id),
            item => Assert.Equal(firstLocalDayMeal.Id, item.Id));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_AppliesStructuredFilters() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-filters-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        (Meal breakfastWithImage, Meal lunchWithAi, Meal dinnerNoImage) =
            await SeedMealFiltersAsync(context, user.Id);
        var repository = new MealRepository(context);

        IReadOnlyList<MealId> mealTypeIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            MealTypes: [MealType.Breakfast, MealType.Lunch]));
        IReadOnlyList<MealId> calorieIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            CaloriesFrom: 600,
            CaloriesTo: 700));
        IReadOnlyList<MealId> withImageIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasImage: true));
        IReadOnlyList<MealId> withoutImageIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasImage: false));
        IReadOnlyList<MealId> withAiIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasAiSession: true));
        IReadOnlyList<MealId> withoutAiIds = await GetMealIdsAsync(repository, user.Id, new MealQueryFilters(
            DateFrom: null,
            DateTo: null,
            HasAiSession: false));

        AssertIds([breakfastWithImage.Id, lunchWithAi.Id], mealTypeIds);
        Assert.Equal([lunchWithAi.Id], calorieIds);
        Assert.Equal([breakfastWithImage.Id], withImageIds);
        AssertIds([lunchWithAi.Id, dinnerNoImage.Id], withoutImageIds);
        Assert.Equal([lunchWithAi.Id], withAiIds);
        AssertIds([breakfastWithImage.Id, dinnerNoImage.Id], withoutAiIds);
    }

    [RequiresDockerFact]
    public async Task GetDistinctMealDatesAsync_ReturnsDistinctDaysForTimedMeals() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-dates-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Meals.AddRange(
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 8, 15, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 2, 21, 30, 0, DateTimeKind.Utc)),
            Meal.Create(user.Id, new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<DateTime> dates = await repository.GetDistinctMealDatesAsync(
            user.Id,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.Collection(
            dates,
            date => Assert.Equal(new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), date),
            date => Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), date));
    }

    [RequiresDockerFact]
    public async Task GetWithItemsAndProductsAsync_FindsMealsByDayWhenDateHasTime() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-products-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 2, 13, 45, 0, DateTimeKind.Utc));
        var otherDayMeal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));

        context.Meals.AddRange(meal, otherDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<Meal> meals = await repository.GetWithItemsAndProductsAsync(
            user.Id,
            new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc));

        Meal actualMeal = Assert.Single(meals);
        Assert.Equal(meal.Id, actualMeal.Id);
    }

    [RequiresDockerFact]
    public async Task GetProductNutritionReadModelsAsync_ProjectsProductItemsForDay() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-product-nutrition-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UsdaFoods.Add(new UsdaFood {
            FdcId = 10,
            Description = "Spinach",
            FoodCategory = "Vegetables",
        });
        await context.SaveChangesAsync();

        var linkedProduct = Product.Create(user.Id, "Spinach", MeasurementUnit.G, 100, 50, 23, 2.9, 0.4, 3.6, 2.2, 0);
        linkedProduct.LinkToUsdaFood(10);
        var unlinkedProduct = Product.Create(user.Id, "Rice", MeasurementUnit.G, 100, 100, 130, 2.7, 0.3, 28, 0.4, 0);
        context.Products.AddRange(linkedProduct, unlinkedProduct);
        await context.SaveChangesAsync();

        var meal = Meal.Create(user.Id, new DateTime(2026, 5, 2, 13, 45, 0, DateTimeKind.Utc));
        meal.AddProduct(linkedProduct.Id, 50);
        meal.AddProduct(unlinkedProduct.Id, 125);
        var otherDayMeal = Meal.Create(user.Id, new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc));
        otherDayMeal.AddProduct(linkedProduct.Id, 75);
        context.Meals.AddRange(meal, otherDayMeal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<UsdaMealProductNutritionReadModel> items = await repository.GetProductNutritionReadModelsAsync(
            user.Id,
            new DateTime(2026, 5, 2, 23, 30, 0, DateTimeKind.Utc),
            limit: 10);

        Assert.Collection(
            items.OrderBy(item => item.Amount),
            linkedItem => {
                Assert.Equal(50, linkedItem.Amount);
                Assert.Equal(100, linkedItem.ProductBaseAmount);
                Assert.Equal(10, linkedItem.UsdaFdcId);
            },
            unlinkedItem => {
                Assert.Equal(125, unlinkedItem.Amount);
                Assert.Equal(100, unlinkedItem.ProductBaseAmount);
                Assert.Null(unlinkedItem.UsdaFdcId);
            });

        IReadOnlyList<UsdaMealProductNutritionReadModel> limitedItems = await repository.GetProductNutritionReadModelsAsync(
            user.Id,
            new DateTime(2026, 5, 2, 23, 30, 0, DateTimeKind.Utc),
            limit: 1);

        Assert.Single(limitedItems);
    }

    [RequiresDockerFact]
    public async Task GetByPeriodAsync_IncludesAiSessionItemsForExport() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"meals-ai-export-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var meal = Meal.Create(
            user.Id,
            new DateTime(2026, 5, 4, 13, 45, 0, DateTimeKind.Utc));
        meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            new DateTime(2026, 5, 4, 13, 46, 0, DateTimeKind.Utc),
            notes: null,
            [
                MealAiItemData.Create("Rice", "Ð Ð¸Ñ", 445, "g", 905, 58, 45, 66, 4, 0),
            ]);

        context.Meals.Add(meal);
        await context.SaveChangesAsync();

        var repository = new MealRepository(context);

        IReadOnlyList<Meal> meals = await repository.GetByPeriodAsync(
            user.Id,
            new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 4, 23, 59, 59, DateTimeKind.Utc));

        Meal actualMeal = Assert.Single(meals);
        MealAiSession session = Assert.Single(actualMeal.AiSessions);
        MealAiItem item = Assert.Single(session.Items);
        Assert.Equal("Rice", item.NameEn);
    }

    private static MealNutritionUpdate CreateManualNutrition(double calories) =>
        new(
            TotalCalories: calories,
            TotalProteins: 1,
            TotalFats: 1,
            TotalCarbs: 1,
            TotalFiber: 0,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: calories,
            ManualProteins: 1,
            ManualFats: 1,
            ManualCarbs: 1,
            ManualFiber: 0,
            ManualAlcohol: 0);

    private static async Task<IReadOnlyList<MealId>> GetMealIdsAsync(
        MealRepository repository,
        UserId userId,
        MealQueryFilters filters) {
        (IReadOnlyList<Meal> items, int _) = await repository.GetPagedAsync(
            userId,
            page: 1,
            limit: 50,
            filters: filters).ConfigureAwait(false);

        return [.. items.Select(item => item.Id)];
    }

    private static async Task<(Meal BreakfastWithImage, Meal LunchWithAi, Meal DinnerNoImage)> SeedMealFiltersAsync(
        FoodDiaryDbContext context,
        UserId userId) {
        var breakfastWithImage = Meal.Create(
            userId,
            new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            MealType.Breakfast,
            imageUrl: "https://cdn.example.com/breakfast.webp");
        breakfastWithImage.ApplyNutrition(CreateManualNutrition(calories: 350));

        var lunchWithAi = Meal.Create(userId, new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc), MealType.Lunch);
        lunchWithAi.ApplyNutrition(CreateManualNutrition(calories: 650));
        lunchWithAi.AddAiSession(imageAssetId: null, AiRecognitionSource.Text, new DateTime(2026, 6, 1, 13, 5, 0, DateTimeKind.Utc), notes: null, items: []);

        var dinnerNoImage = Meal.Create(userId, new DateTime(2026, 6, 1, 19, 0, 0, DateTimeKind.Utc), MealType.Dinner);
        dinnerNoImage.ApplyNutrition(CreateManualNutrition(calories: 820));

        context.Meals.AddRange(breakfastWithImage, lunchWithAi, dinnerNoImage);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return (breakfastWithImage, lunchWithAi, dinnerNoImage);
    }

    private static void AssertIds(IReadOnlyCollection<MealId> expected, IReadOnlyCollection<MealId> actual) =>
        Assert.Equal(
            [.. expected.Select(id => id.Value).Order()],
            [.. actual.Select(id => id.Value).Order()]);
}
