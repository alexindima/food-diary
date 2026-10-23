using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("nested-recipe")]
    [InlineData("product-favorite")]
    public async Task CleanupOwnedFixtures_DependentOwnedAccounts_ArePurgedWithinOneBoundedCall(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runId = Guid.NewGuid();
        var client = User.Create($"webmcp-fixture-{runId:D}-client@example.test", "hash");
        var dietologist = User.Create($"webmcp-fixture-{runId:D}-dietologist@example.test", "hash");
        var outsider = User.Create("unrelated-fixture-outsider@example.test", "hash");
        client.MarkDeleted(DateTime.UtcNow.AddDays(-1));
        dietologist.MarkDeleted(DateTime.UtcNow.AddDays(-1));
        context.Users.AddRange(client, dietologist, outsider);
        await context.SaveChangesAsync();
        User[] ordered = await context.Users.Where(user => user.Id == client.Id || user.Id == dietologist.Id)
            .OrderBy(user => user.Id).ToArrayAsync();
        User source = ordered[0];
        User consumer = ordered[1];
        var product = Product.Create(source.Id, "Fixture dependency product", MeasurementUnit.G, 100,
            defaultPortionAmount: null, 100, 1, 1, 1, 0, 0, visibility: Visibility.Public);
        var recipe = Recipe.Create(source.Id, "Fixture dependency recipe", servings: 1, visibility: Visibility.Public);
        var unrelatedRecipe = Recipe.Create(outsider.Id, "Unrelated outsider recipe", servings: 1, visibility: Visibility.Private);
        context.AddRange(product, recipe, unrelatedRecipe);
        if (string.Equals(reference, "nested-recipe", StringComparison.Ordinal)) {
            var parent = Recipe.Create(consumer.Id, "Fixture consumer recipe", servings: 1);
            parent.AddStep(1, "Owned consumer").AddNestedRecipeIngredient(recipe.Id, RecipeServingQuantity.FromServings(1));
            context.Recipes.Add(parent);
        } else {
            context.FavoriteProducts.Add(FavoriteProduct.Create(consumer.Id, product.Id));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        int removed = await service.CleanupOwnedFixtureUsersAsync(
            runId, [consumer.Id.Value, source.Id.Value], DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, removed);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == source.Id || user.Id == consumer.Id));
        Assert.True(await observer.Users.AnyAsync(user => user.Id == outsider.Id));
        Assert.Equal(unrelatedRecipe.Id, (await observer.Recipes.SingleAsync()).Id);
        Assert.Empty(await observer.Products.ToListAsync());
        Assert.Empty(await observer.FavoriteProducts.ToListAsync());
        Assert.False(await observer.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));
    }
}
