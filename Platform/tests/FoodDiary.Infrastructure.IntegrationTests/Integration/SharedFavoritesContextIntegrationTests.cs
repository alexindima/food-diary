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
