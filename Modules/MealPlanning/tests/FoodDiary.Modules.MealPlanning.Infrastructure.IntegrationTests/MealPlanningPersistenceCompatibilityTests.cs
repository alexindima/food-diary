using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealPlanningPersistenceCompatibilityTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ShoppingListMappings_PreserveScalarUserForeignKeyProvenanceAndDeletionSemantics() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"planning-mapping-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(user.Id, "Rice", MeasurementUnit.G, 100, 100,
            caloriesPerBase: 120, proteinsPerBase: 3, fatsPerBase: 1, carbsPerBase: 20,
            fiberPerBase: 2, alcoholPerBase: 0);
        var list = ShoppingList.Create(user.Id, "Weekly");
        ShoppingListItem item = list.AddItem("Rice", product.Id, 250, MeasurementUnit.G, "Pantry", isChecked: false, 0);
        // Provenance is a snapshot: these IDs deliberately have no corresponding source rows.
        var sourcePlanId = MealPlanId.New();
        var sourceMealId = MealPlanMealId.New();
        var sourceRecipeId = RecipeId.New();
        item.AddMealPlanSource(sourcePlanId, sourceMealId, sourceRecipeId,
            "Day 1 lunch", 1, "Lunch", 250, MeasurementUnit.G);
        context.Users.Add(user);
        context.Products.Add(product);
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ShoppingList savedList = await context.ShoppingLists
            .SingleAsync(value => value.Id == list.Id);
        Assert.Equal(user.Id, savedList.UserId);
        ShoppingListItem savedItem = await context.Set<ShoppingListItem>().Include(value => value.Sources)
            .SingleAsync(value => value.Id == item.Id);
        ShoppingListItemSource savedSource = Assert.Single(savedItem.Sources);
        Assert.Multiple(
            () => Assert.Equal(sourcePlanId, savedSource.MealPlanId),
            () => Assert.Equal(sourceMealId, savedSource.MealPlanMealId),
            () => Assert.Equal(sourceRecipeId, savedSource.RecipeId),
            () => Assert.Equal(250, savedSource.Amount),
            () => Assert.Equal(MeasurementUnit.G, savedSource.Unit));

        await context.Products.Where(value => value.Id == product.Id).ExecuteDeleteAsync();
        context.ChangeTracker.Clear();
        savedItem = await context.Set<ShoppingListItem>().SingleAsync(value => value.Id == item.Id);
        Assert.Null(savedItem.ProductId);
        Assert.True(await context.Set<ShoppingListItemSource>().AnyAsync(value => value.Id == savedSource.Id));

        await context.ShoppingLists.Where(value => value.Id == list.Id).ExecuteDeleteAsync();
        Assert.False(await context.Set<ShoppingListItem>().AnyAsync(value => value.Id == item.Id));
        Assert.False(await context.Set<ShoppingListItemSource>().AnyAsync(value => value.Id == savedSource.Id));
        Assert.True(await context.Users.AnyAsync(value => value.Id == user.Id));
    }
}
