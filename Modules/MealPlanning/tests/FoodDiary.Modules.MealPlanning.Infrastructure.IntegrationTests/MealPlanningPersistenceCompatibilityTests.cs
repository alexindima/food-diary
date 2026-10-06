using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealPlanningPersistenceCompatibilityTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DeletePersonalPlan_ProtectsForeignAndCuratedAndPreservesRecipesProductsAndShoppingSources() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"plan-delete-owner-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"plan-delete-other-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(owner.Id, "Rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        var recipe = FoodDiary.Modules.Recipes.Domain.Entities.Recipe.Create(owner.Id, "Rice dish", 2);
        recipe.AddStep(1, "Cook").AddProductIngredient(product.Id, 250);
        var template = MealPlan.CreateCurated("Template", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        MealPlan personal = template.Adopt(owner.Id);
        MealPlanDay day = personal.AddDay(1);
        MealPlanMeal meal = day.AddMeal(MealType.Lunch, recipe.Id, 1);
        MealPlan foreign = template.Adopt(other.Id);
        var list = ShoppingList.Create(owner.Id, "Keep this list");
        ShoppingListItem item = list.AddItem("Rice", product.Id, 250, MeasurementUnit.G, category: null, isChecked: false, 0);
        item.AddMealPlanSource(personal.Id, meal.Id, recipe.Id, "Lunch", 1, "Lunch", 250, MeasurementUnit.G);
        context.AddRange(owner, other, product, recipe, template, personal, foreign, list);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans.MealPlanRepository(
            context.MealPlans, new FoodDiary.ReadModel.Composition.MealPlanning.MealPlanCompositionReader(context));

        Assert.False(await repository.DeletePersonalAsync(foreign.Id, owner.Id));
        Assert.False(await repository.DeletePersonalAsync(template.Id, owner.Id));
        Assert.True(await repository.DeletePersonalAsync(personal.Id, owner.Id));
        Assert.True(await context.MealPlans.AsNoTracking().AnyAsync(plan => plan.Id == personal.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ShoppingListItemSource source = await context.Set<ShoppingListItemSource>().SingleAsync(value => value.ShoppingListItemId == item.Id);
        Assert.Multiple(
            () => Assert.Equal(personal.Id, source.MealPlanId),
            () => Assert.Equal(meal.Id, source.MealPlanMealId),
            () => Assert.Equal(recipe.Id, source.RecipeId));
        Assert.False(await context.MealPlans.AnyAsync(plan => plan.Id == personal.Id));
        Assert.False(await context.Set<FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlanDay>().AnyAsync(value => value.Id == day.Id));
        Assert.False(await context.Set<FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlanMeal>().AnyAsync(value => value.Id == meal.Id));
        Assert.True(await context.MealPlans.AnyAsync(plan => plan.Id == foreign.Id));
        Assert.True(await context.MealPlans.AnyAsync(plan => plan.Id == template.Id));
        Assert.True(await context.Recipes.AnyAsync(value => value.Id == recipe.Id));
        Assert.True(await context.Products.AnyAsync(value => value.Id == product.Id));
        Assert.True(await context.ShoppingLists.AnyAsync(value => value.Id == list.Id));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.DeletePersonalAsync(foreign.Id, other.Id, cancellation.Token));
        Assert.False(await repository.DeletePersonalAsync(personal.Id, owner.Id));
    }

    [RequiresDockerFact]
    public async Task GetPageSummaryReadModels_FiltersOwnedAndCuratedPlansBeforeCountingAndPaging() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"plan-filter-owner-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"plan-filter-other-{Guid.NewGuid():N}@example.com", "hash");
        var ownBalanced = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateForUser(owner.Id, "Own balanced", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        var ownKeto = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateForUser(owner.Id, "Own keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        var foreignKeto = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateForUser(other.Id, "Private keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        var curatedBalanced = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateCurated("Curated balanced", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        var curatedKeto = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateCurated("Curated keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        context.AddRange(owner, other, ownBalanced, ownKeto, foreignKeto, curatedBalanced, curatedKeto);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans.MealPlanRepository(context.MealPlans, new FoodDiary.ReadModel.Composition.MealPlanning.MealPlanCompositionReader(context));

        (IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> firstItems, int firstTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 1, limit: 1);
        (IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> secondItems, int secondTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 2, limit: 1);
        (IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> beyondItems, int beyondTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 3, limit: 1);
        (IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> balancedItems, int balancedTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Balanced, page: 1, limit: 50);
        (IReadOnlyList<FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models.MealPlanSummaryReadModel> allItems, int allTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, dietType: null, page: 1, limit: 50);

        Guid firstId = Assert.Single(firstItems).Id;
        Guid secondId = Assert.Single(secondItems).Id;
        Assert.Multiple(
            () => Assert.Equal(2, firstTotal),
            () => Assert.Equal(curatedKeto.Id.Value, firstId),
            () => Assert.Equal(2, secondTotal),
            () => Assert.Equal(ownKeto.Id.Value, secondId),
            () => Assert.Equal(2, beyondTotal),
            () => Assert.Empty(beyondItems),
            () => Assert.Equal(2, balancedTotal),
            () => Assert.Equal(new[] { curatedBalanced.Id.Value, ownBalanced.Id.Value }, balancedItems.Select(plan => plan.Id)),
            () => Assert.Equal(4, allTotal),
            () => Assert.Equal(4, allItems.Count),
            () => Assert.DoesNotContain(allItems, plan => plan.Id == foreignKeto.Id),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetPlanWithDays_LoadsProductIngredientSnapshotsForEachRecipe() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"plan-snapshot-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(user.Id, "Rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        var recipe = FoodDiary.Modules.Recipes.Domain.Entities.Recipe.Create(user.Id, "Rice dish", 2);
        recipe.AddStep(1, "Cook").AddProductIngredient(product.Id, 250);
        var plan = FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan.CreateForUser(user.Id, "Week", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Lunch, recipe.Id, 1);
        context.AddRange(user, product, recipe, plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlan? loaded = await new FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans.MealPlanRepository(context.MealPlans, new FoodDiary.ReadModel.Composition.MealPlanning.MealPlanCompositionReader(context)).GetByIdAsync(plan.Id, includeDays: true);

        Assert.NotNull(loaded);
        FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlanRecipeSnapshot? snapshot = Assert.Single(Assert.Single(loaded.Days).Meals).RecipeSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(recipe.Id, snapshot.Id);
        Assert.Equal("Rice dish", snapshot.Name);
        Assert.Equal(2, snapshot.Servings);
        FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans.MealPlanRecipeIngredientSnapshot ingredient = Assert.Single(snapshot.Ingredients);
        Assert.Equal(product.Id, ingredient.ProductId);
        Assert.Equal(250, ingredient.Amount);
        Assert.Equal("Rice", ingredient.Name);
        Assert.Empty(context.ChangeTracker.Entries());
    }
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
