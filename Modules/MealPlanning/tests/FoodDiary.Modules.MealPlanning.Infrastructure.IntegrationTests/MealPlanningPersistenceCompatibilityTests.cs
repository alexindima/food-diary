using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.MealPlans;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.ReadModel.Composition.MealPlanning;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
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
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Modules.MealPlanning.Application;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.ShoppingLists;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.IntegrationTests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealPlanningPersistenceCompatibilityTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ShoppingSnapshots_UseRequestingViewerAndKeepOnlyPublishedPrivateIngredientDescriptions() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var author = User.Create($"plan-author-{Guid.NewGuid():N}@example.com", "hash");
        var viewer = User.Create($"plan-viewer-{Guid.NewGuid():N}@example.com", "hash");
        var privateProduct = Product.Create(author.Id, "Secret supplier product", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        var viewerProduct = Product.Create(viewer.Id, "Viewer rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        privateProduct.ChangeVisibility(Visibility.Private);
        viewerProduct.ChangeVisibility(Visibility.Private);
        var publicProduct = Product.Create(author.Id, "Public rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        publicProduct.ChangeVisibility(Visibility.Public);
        var recipe = Recipe.Create(author.Id, "Published dish", 2);
        recipe.ChangeVisibility(Visibility.Public);
        RecipeStep step = recipe.AddStep(1, "Cook");
        step.AddProductIngredient(privateProduct.Id, ProductUnitQuantity.FromUnits(250)).SetPublicDescription("Published ingredient", "g");
        step.AddProductIngredient(viewerProduct.Id, ProductUnitQuantity.FromUnits(100));
        step.AddProductIngredient(publicProduct.Id, ProductUnitQuantity.FromUnits(50));
        var plan = MealPlan.CreateCurated("Curated", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Lunch, recipe.Id, 1);
        context.AddRange(author, viewer, privateProduct, viewerProduct, publicProduct, recipe, plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new MealPlanCompositionReader(context);
        var repository = new MealPlanRepository(context.MealPlans, reader);
        MealPlan loaded = (await repository.GetAccessibleByIdAsync(plan.Id, viewer.Id, includeDays: true))!;
        MealPlanRecipeSnapshot snapshot = Assert.Single(Assert.Single(loaded.Days).Meals).RecipeSnapshot!;
        Assert.Equal(3, snapshot.Ingredients.Count);
        Assert.Contains(snapshot.Ingredients, item => item.ProductId == viewerProduct.Id && string.Equals(item.Name, viewerProduct.Name, StringComparison.Ordinal));
        Assert.Contains(snapshot.Ingredients, item => item.ProductId == publicProduct.Id);
        MealPlanRecipeIngredientSnapshot hidden = Assert.Single(snapshot.Ingredients, item => item.ProductId is null);
        Assert.Equal("Published ingredient", hidden.Name);
        Assert.Equal(MeasurementUnit.G, hidden.BaseUnit);
        Assert.Null(hidden.Category);
        Assert.DoesNotContain(snapshot.Ingredients, item => item.ProductId == privateProduct.Id || string.Equals(item.Name, privateProduct.Name, StringComparison.Ordinal));
        MealPlanRecipeSnapshot authorSnapshot = (await reader.GetRecipeSnapshotsAsync([recipe.Id], author.Id))[recipe.Id];
        Assert.Contains(authorSnapshot.Ingredients, item => item.ProductId == privateProduct.Id);
        Assert.DoesNotContain((await reader.GetRecipeSnapshotsAsync([recipe.Id], viewerId: null))[recipe.Id].Ingredients, item => item.ProductId == viewerProduct.Id);

        var services = new ServiceCollection();
        services.AddMealPlanningApplication();
        services.AddSingleton<IShoppingListWriteRepository>(new ShoppingListRepository(context.ShoppingLists));
        await using ServiceProvider provider = services.BuildServiceProvider();
        var handler = new GenerateShoppingListCommandHandler(repository,
            provider.GetRequiredService<IShoppingListCreationService>(), new OwnedUserAccess(viewer.Id));
        Result<ShoppingListModel> generated = await handler.Handle(
            new GenerateShoppingListCommand(viewer.Id.Value, plan.Id.Value), CancellationToken.None);
        Assert.True(generated.IsSuccess, generated.Error.Message);
        ShoppingListItemModel publishedItem = Assert.Single(generated.Value.Items, item => item.ProductId is null);
        Assert.Equal("Published ingredient", publishedItem.Name);
        Assert.Equal(125, publishedItem.Amount);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        ShoppingList saved = (await new ShoppingListRepository(context.ShoppingLists)
            .GetByIdAsync(new ShoppingListId(generated.Value.Id), viewer.Id, includeItems: true))!;
        Assert.DoesNotContain(saved.Items, item => item.ProductId == privateProduct.Id || string.Equals(item.Name, privateProduct.Name, StringComparison.Ordinal));
        Assert.Contains(saved.Items, item => item.ProductId == viewerProduct.Id);
    }

    [ExcludeFromCodeCoverage]
    private sealed class OwnedUserAccess(UserId owner) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Assert.Equal(owner, userId);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Error?>(null);
        }
    }

    [RequiresDockerFact]
    public async Task DeletePersonalPlan_ProtectsForeignAndCuratedAndPreservesRecipesProductsAndShoppingSources() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"plan-delete-owner-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"plan-delete-other-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(owner.Id, "Rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        var recipe = Recipe.Create(owner.Id, "Rice dish", 2);
        recipe.AddStep(1, "Cook").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(250));
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
        var repository = new MealPlanRepository(
            context.MealPlans, new MealPlanCompositionReader(context));

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
        Assert.False(await context.Set<MealPlanDay>().AnyAsync(value => value.Id == day.Id));
        Assert.False(await context.Set<MealPlanMeal>().AnyAsync(value => value.Id == meal.Id));
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
        var ownBalanced = MealPlan.CreateForUser(owner.Id, "Own balanced", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        var ownKeto = MealPlan.CreateForUser(owner.Id, "Own keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        var foreignKeto = MealPlan.CreateForUser(other.Id, "Private keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        var curatedBalanced = MealPlan.CreateCurated("Curated balanced", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        var curatedKeto = MealPlan.CreateCurated("Curated keto", description: null, DietType.Keto, durationDays: 1, targetCaloriesPerDay: null);
        context.AddRange(owner, other, ownBalanced, ownKeto, foreignKeto, curatedBalanced, curatedKeto);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new MealPlanRepository(context.MealPlans, new MealPlanCompositionReader(context));

        (IReadOnlyList<MealPlanSummaryReadModel> firstItems, int firstTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 1, limit: 1);
        (IReadOnlyList<MealPlanSummaryReadModel> secondItems, int secondTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 2, limit: 1);
        (IReadOnlyList<MealPlanSummaryReadModel> beyondItems, int beyondTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 3, limit: 1);
        (IReadOnlyList<MealPlanSummaryReadModel> balancedItems, int balancedTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Balanced, page: 1, limit: 50);
        (IReadOnlyList<MealPlanSummaryReadModel> allItems, int allTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, dietType: null, page: 1, limit: 50);
        (IReadOnlyList<MealPlanSummaryReadModel> savedItems, int savedTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 1, limit: 1, isCurated: false);
        (IReadOnlyList<MealPlanSummaryReadModel> savedBeyond, int savedBeyondTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 2, limit: 1, isCurated: false);
        (IReadOnlyList<MealPlanSummaryReadModel> curatedItems, int curatedTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, DietType.Keto, page: 1, limit: 1, isCurated: true);
        (IReadOnlyList<MealPlanSummaryReadModel> allSavedItems, int allSavedTotal) = await repository.GetPageSummaryReadModelsAsync(owner.Id, dietType: null, page: 1, limit: 50, isCurated: false);

        Guid firstId = Assert.Single(firstItems).Id;
        Guid secondId = Assert.Single(secondItems).Id;
        Guid savedId = Assert.Single(savedItems).Id;
        Guid curatedId = Assert.Single(curatedItems).Id;
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
            () => Assert.Equal(1, savedTotal),
            () => Assert.Equal(ownKeto.Id.Value, savedId),
            () => Assert.Equal(1, savedBeyondTotal),
            () => Assert.Empty(savedBeyond),
            () => Assert.Equal(1, curatedTotal),
            () => Assert.Equal(curatedKeto.Id.Value, curatedId),
            () => Assert.Equal(2, allSavedTotal),
            () => Assert.All(allSavedItems, plan => Assert.False(plan.IsCurated)),
            () => Assert.DoesNotContain(allSavedItems, plan => plan.Id == foreignKeto.Id),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetPlanWithDays_LoadsProductIngredientSnapshotsForEachRecipe() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"plan-snapshot-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(user.Id, "Rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        var recipe = Recipe.Create(user.Id, "Rice dish", 2);
        recipe.AddStep(1, "Cook").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(250));
        var plan = MealPlan.CreateForUser(user.Id, "Week", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Lunch, recipe.Id, 1);
        context.AddRange(user, product, recipe, plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        MealPlan? loaded = await new MealPlanRepository(context.MealPlans, new MealPlanCompositionReader(context)).GetByIdAsync(plan.Id, includeDays: true);

        Assert.NotNull(loaded);
        MealPlanRecipeSnapshot? snapshot = Assert.Single(Assert.Single(loaded.Days).Meals).RecipeSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(recipe.Id, snapshot.Id);
        Assert.Equal("Rice dish", snapshot.Name);
        Assert.Equal(2, snapshot.Servings);
        MealPlanRecipeIngredientSnapshot ingredient = Assert.Single(snapshot.Ingredients);
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
