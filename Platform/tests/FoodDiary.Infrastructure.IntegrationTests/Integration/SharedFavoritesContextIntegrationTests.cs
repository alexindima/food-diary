using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Infrastructure;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedFavoritesContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RemovedMealsDisappearFromAllReadsAndRestoreOriginalPagedOrderAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        var owner = User.Create("restore-owner@example.com", "hash");
        var other = User.Create("restore-other@example.com", "hash");
        context.Users.AddRange(owner, other);
        for (int index = 0; index < 21; index++) {
            var meal = Meal.Create(owner.Id, DateTime.UtcNow);
            context.Meals.Add(meal);
            context.FavoriteMeals.Add(FavoriteMeal.Create(owner.Id, meal.Id, "Lunch"));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IFavoriteMealQuery query = provider.GetRequiredService<IFavoriteMealQuery>();
        IFavoriteMealWriteRepository repository = provider.GetRequiredService<IFavoriteMealWriteRepository>();
        IFavoriteMealReadRepository reads = provider.GetRequiredService<IFavoriteMealReadRepository>();
        IUnitOfWork unit = provider.GetRequiredService<IUnitOfWork>();
        (IReadOnlyList<FavoriteMealReadModel> before, _) = await query.GetPageReadModelsAsync(owner.Id, 2, 10, "Lunch");
        FavoriteMealReadModel original = before[4];
        var id = new FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids.FavoriteMealId(original.Id);
        var mealId = new FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids.MealId(original.MealId);
        FavoriteMeal favorite = Assert.IsType<FavoriteMeal>(await repository.GetByIdAsync(id, owner.Id, asTracking: true));
        await repository.DeleteAsync(favorite);
        await unit.SaveChangesAsync();
        Assert.Null(await repository.GetByIdAsync(id, owner.Id));
        Assert.Null(await repository.GetByMealIdAsync(mealId, owner.Id));
        Assert.False(await reads.ExistsByMealIdAsync(mealId, owner.Id));
        Assert.Empty(await reads.GetFavoriteIdsByMealIdsAsync(owner.Id, [mealId]));
        Assert.Equal(20, (await query.GetOverviewReadModelsAsync(owner.Id, 0)).TotalItems);
        Assert.DoesNotContain(await query.GetAllReadModelsAsync(owner.Id), item => item.Id == original.Id);
        Assert.Null(await repository.GetForRestoreAsync(id, other.Id));
        FavoriteMeal removed = Assert.IsType<FavoriteMeal>(await repository.GetForRestoreAsync(id, owner.Id));
        Assert.NotNull(removed.RemovedAtUtc);
        removed.Restore();
        await unit.SaveChangesAsync();
        (IReadOnlyList<FavoriteMealReadModel> after, int total) = await query.GetPageReadModelsAsync(owner.Id, 2, 10, "Lunch");
        Assert.Equal(before.Select(item => item.Id), after.Select(item => item.Id));
        Assert.Equal(original.CreatedAtUtc, after[4].CreatedAtUtc);
        Assert.Equal(21, total);
        Assert.Empty(context.ChangeTracker.Entries());

        await repository.DeleteAsync(removed);
        await unit.SaveChangesAsync();
        var replacement = FavoriteMeal.Create(owner.Id, mealId, "Added again");
        await repository.AddAsync(replacement);
        await unit.SaveChangesAsync();
        Assert.Equal(replacement.Id, (await repository.GetByMealIdAsync(mealId, owner.Id))!.Id);
        removed.Restore();
        await Assert.ThrowsAsync<DbUpdateException>(() => unit.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task MealPickerPagesSearchesSnapshotsAndIsolatesUsersAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        var owner = User.Create("picker-owner@example.com", "hash");
        var other = User.Create("picker-other@example.com", "hash");
        context.Users.AddRange(owner, other);
        var product = Product.Create(owner.Id, "Rice", MeasurementUnit.G, 100, 100, 130, 2, 1, 28, 1, 0, imageUrl: "https://example.com/product.jpg");
        context.Products.Add(product);
        var recipe = Recipe.Create(owner.Id, "Soup", servings: 1, imageUrl: "https://example.com/recipe.jpg");
        context.Recipes.Add(recipe);
        var cover = FoodDiary.Modules.Images.Domain.Entities.Assets.ImageAsset.Create(owner.Id, "favorite-cover", "https://example.com/cover.jpg");
        var aiCover = FoodDiary.Modules.Images.Domain.Entities.Assets.ImageAsset.Create(owner.Id, "favorite-ai", "https://example.com/ai.jpg");
        context.ImageAssets.AddRange(cover, aiCover);
        for (int index = 0; index < 23; index++) {
            var meal = Meal.Create(owner.Id, DateTime.UtcNow, imageUrl: index == 22 ? "https://example.com/meal.jpg" : null,
                imageAssetId: index == 0 || index == 22 ? cover.Id : null);
            meal.ApplyNutrition(new FoodDiary.Modules.Meals.Domain.ValueObjects.MealNutritionUpdate(130, 2, 1, 28, 7.5, 0, IsAutoCalculated: true));
            MealItem mealItem = index == 1 ? meal.AddRecipe(recipe.Id, 1) : meal.AddProduct(product.Id, 100);
            if (index > 1) {
                mealItem.ApplyProductSnapshot("Рис 100%_готовый", imageUrl: "https://example.com/rice.jpg", MeasurementUnit.G, 100, 130, 2, 1, 28, 1, 0);
            }
            if (index == 2) {
                meal.AddAiSession(imageAssetId: aiCover.Id, FoodDiary.Modules.Meals.Domain.Contracts.Enums.AiRecognitionSource.Text,
                    DateTime.UtcNow, notes: null, [new MealAiItemData("Coffee", "Кофе", 100, "ml", 5, 0, 0, 0, 0, 0)]);
            }
            context.Meals.Add(meal);
            context.FavoriteMeals.Add(FavoriteMeal.Create(owner.Id, meal.Id, "Favorite " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        var otherMeal = Meal.Create(other.Id, DateTime.UtcNow);
        context.Meals.Add(otherMeal);
        context.FavoriteMeals.Add(FavoriteMeal.Create(other.Id, otherMeal.Id, "Hidden favorite"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IFavoriteMealQuery query = provider.GetRequiredService<IFavoriteMealQuery>();
        (IReadOnlyList<FavoriteMealReadModel> firstItems, int firstTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, search: null);
        (IReadOnlyList<FavoriteMealReadModel> secondItems, int secondTotal) = await query.GetPageReadModelsAsync(owner.Id, 2, 10, search: null);
        (IReadOnlyList<FavoriteMealReadModel> thirdItems, int thirdTotal) = await query.GetPageReadModelsAsync(owner.Id, 3, 10, search: null);
        (IReadOnlyList<FavoriteMealReadModel> titleItems, int titleTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "FAVORITE 22");
        (IReadOnlyList<FavoriteMealReadModel> ingredientItems, int ingredientTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, " рис ");
        (IReadOnlyList<FavoriteMealReadModel> literalItems, int literalTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "%_");
        (IReadOnlyList<FavoriteMealReadModel> missingItems, int missingTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "Hidden");
        (IReadOnlyList<FavoriteMealReadModel> beyondItems, int beyondTotal) = await query.GetPageReadModelsAsync(owner.Id, 9, 10, search: null);
        (IReadOnlyList<FavoriteMealReadModel> legacyItems, int legacyTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "rice");
        (IReadOnlyList<FavoriteMealReadModel> recipeItems, int recipeTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "SOUP");
        (IReadOnlyList<FavoriteMealReadModel> aiItems, int aiTotal) = await query.GetPageReadModelsAsync(owner.Id, 1, 10, "кофе");
        Assert.Multiple(
            () => Assert.Equal(1, aiTotal),
            () => Assert.Equal("Кофе", aiItems.Single().AiItemNames.Single()),
            () => Assert.Equal(1, recipeTotal),
            () => Assert.Equal("Soup", recipeItems.Single().ItemNames.Single()),
            () => Assert.Equal(1, legacyTotal),
            () => Assert.Equal("Rice", legacyItems.Single().ItemNames.Single()),
            () => Assert.Equal(23, firstTotal),
            () => Assert.Equal(10, firstItems.Count),
            () => Assert.Equal(10, secondItems.Count),
            () => Assert.Equal(3, thirdItems.Count),
            () => Assert.Equal(23, firstItems.Concat(secondItems).Concat(thirdItems).Select(item => item.Id).Distinct().Count()),
            () => Assert.Equal("https://example.com/meal.jpg", titleItems.Single().ImageUrl),
            () => Assert.Null(recipeItems.Single().ImageUrl),
            () => Assert.Equal(new[] { "https://example.com/product.jpg" }, legacyItems.Single().ItemImageUrls),
            () => Assert.Equal(new[] { "https://example.com/recipe.jpg" }, recipeItems.Single().ItemImageUrls),
            () => Assert.Equal(new[] { "https://example.com/rice.jpg" }, titleItems.Single().ItemImageUrls),
            () => Assert.Equal("https://example.com/cover.jpg", legacyItems.Single().ImageUrl),
            () => Assert.Equal(new[] { "https://example.com/ai.jpg" }, aiItems.Single().AiImageUrls),
            () => Assert.Equal(7.5, titleItems.Single().TotalFiber),
            () => Assert.Equal(1, titleTotal),
            () => Assert.Equal(21, ingredientTotal),
            () => Assert.Equal(21, literalTotal),
            () => Assert.Equal("Рис 100%_готовый", firstItems[0].ItemNames.Single()),
            () => Assert.Empty(missingItems),
            () => Assert.Empty(beyondItems),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task MealOverviewCountsBeyondCollectionLimitAndScopesRowsToUserAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        var owner = User.Create("favorite-overview@example.com", "hash");
        var other = User.Create("favorite-overview-other@example.com", "hash");
        context.Users.AddRange(owner, other);
        for (int index = 0; index < 1_001; index++) {
            var meal = Meal.Create(owner.Id, DateTime.UtcNow);
            context.Meals.Add(meal);
            context.FavoriteMeals.Add(FavoriteMeal.Create(owner.Id, meal.Id));
        }
        var otherMeal = Meal.Create(other.Id, DateTime.UtcNow);
        context.Meals.Add(otherMeal);
        context.FavoriteMeals.Add(FavoriteMeal.Create(other.Id, otherMeal.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IFavoriteMealReadModelRepository repository = provider.GetRequiredService<IFavoriteMealReadModelRepository>();

        (IReadOnlyList<FavoriteMealReadModel> items, int totalItems) = await repository.GetOverviewReadModelsAsync(owner.Id, 2);
        (IReadOnlyList<FavoriteMealReadModel> emptyItems, int emptyTotal) = await repository.GetOverviewReadModelsAsync(owner.Id, 0);

        Assert.Multiple(
            () => Assert.Equal(1_001, totalItems),
            () => Assert.Equal(2, items.Count),
            () => Assert.DoesNotContain(items, item => item.MealId == otherMeal.Id.Value),
            () => Assert.Empty(emptyItems),
            () => Assert.Equal(1_001, emptyTotal),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task SharedSaveTracksOnlyFavoritesAndVisibilityChangesRevokeReadsAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        var owner = User.Create("favorites-owner@example.com", "hash");
        var reader = User.Create("favorites-reader@example.com", "hash");
        var product = Product.Create(owner.Id, "Rice", MeasurementUnit.G, 100, 100, 130, 2, 1, 28, 1, 0, visibility: Visibility.Public);
        var recipe = Recipe.Create(owner.Id, "Rice bowl", servings: 2, visibility: Visibility.Public);
        context.Users.AddRange(owner, reader);
        context.Products.Add(product);
        context.Recipes.Add(recipe);
        IFavoriteProductWriteRepository products = provider.GetRequiredService<IFavoriteProductWriteRepository>();
        IFavoriteRecipeWriteRepository recipes = provider.GetRequiredService<IFavoriteRecipeWriteRepository>();
        var favoriteProduct = FavoriteProduct.Create(reader.Id, product.Id);
        var favoriteRecipe = FavoriteRecipe.Create(reader.Id, recipe.Id);
        await products.AddAsync(favoriteProduct);
        await recipes.AddAsync(favoriteRecipe);
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync();
        FavoritesDbContext owned = provider.GetRequiredService<FavoritesDbContext>();
        Assert.Equal(3, owned.Model.GetEntityTypes().Count());
        Assert.Same(context.Database.GetDbConnection(), owned.Database.GetDbConnection());
        context.ChangeTracker.Clear();
        owned.ChangeTracker.Clear();
        FavoriteProduct? trackedProduct = await products.GetByIdAsync(favoriteProduct.Id, reader.Id, asTracking: true);
        FavoriteRecipe? trackedRecipe = await recipes.GetByIdAsync(favoriteRecipe.Id, reader.Id, asTracking: true);
        Assert.NotNull(trackedProduct);
        Assert.NotNull(trackedRecipe);
        Assert.Same(trackedProduct, await products.GetByIdAsync(favoriteProduct.Id, reader.Id, asTracking: true));
        Assert.Same(trackedRecipe, await recipes.GetByIdAsync(favoriteRecipe.Id, reader.Id, asTracking: true));
        Assert.Empty(context.ChangeTracker.Entries());
        trackedProduct.UpdateName("Lunch");
        trackedRecipe.UpdateName("Dinner");
        await unitOfWork.SaveChangesAsync();
        Assert.Equal("Lunch", (await context.FavoriteProducts.AsNoTracking().SingleAsync()).Name);
        Assert.Equal("Dinner", (await context.FavoriteRecipes.AsNoTracking().SingleAsync()).Name);
        product = await context.Products.SingleAsync(source => source.Id == product.Id);
        recipe = await context.Recipes.SingleAsync(source => source.Id == recipe.Id);
        product.ChangeVisibility(Visibility.Private);
        recipe.ChangeVisibility(Visibility.Private);
        await unitOfWork.SaveChangesAsync();
        Assert.Null(await products.GetByIdAsync(favoriteProduct.Id, reader.Id, asTracking: true));
        Assert.Null(await recipes.GetByIdAsync(favoriteRecipe.Id, reader.Id, asTracking: true));
        Assert.Empty(await provider.GetRequiredService<IFavoriteProductReadModelRepository>().GetAllReadModelsAsync(reader.Id));
        Assert.Empty(await provider.GetRequiredService<IFavoriteRecipeReadModelRepository>().GetAllReadModelsAsync(reader.Id));
        Assert.NotNull(await products.GetOwnedByIdAsync(favoriteProduct.Id, reader.Id, asTracking: true));
        Assert.NotNull(await recipes.GetOwnedByIdAsync(favoriteRecipe.Id, reader.Id, asTracking: true));
        Assert.Null(await products.GetOwnedByIdAsync(favoriteProduct.Id, owner.Id));
        await products.DeleteAsync(trackedProduct);
        await recipes.DeleteAsync(trackedRecipe);
        await unitOfWork.SaveChangesAsync();
        Assert.Empty(await context.FavoriteProducts.AsNoTracking().ToListAsync());
        Assert.Empty(await context.FavoriteRecipes.AsNoTracking().ToListAsync());
    }

    [RequiresDockerFact]
    public async Task MissingSourceRollsBackSharedUserAndFavoriteAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        var user = User.Create("favorite-rollback@example.com", "hash");
        context.Users.Add(user);
        await provider.GetRequiredService<IFavoriteProductWriteRepository>().AddAsync(FavoriteProduct.Create(user.Id, ProductId.New()));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Empty(await verification.Users.ToListAsync());
        Assert.Empty(await verification.FavoriteProducts.ToListAsync());
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddFavoritesModule();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
