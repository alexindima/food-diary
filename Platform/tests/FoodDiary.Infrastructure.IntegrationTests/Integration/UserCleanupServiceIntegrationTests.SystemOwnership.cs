using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class UserCleanupServiceIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupDeletedUsersAsync_WithoutSuccessor_RetainsReferencedClosureAndPurgesUnrelatedContent(bool privateRoot) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        SystemOwnershipScenario scenario = await SeedSystemOwnershipScenarioAsync(context, privateRoot);
        var outbox = new RecordingImageObjectDeletionOutbox();
        await using ServiceProvider provider = CreateServiceProvider(context, outbox, out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(1, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        User system = await observer.Users.AsNoTracking().Include(user => user.UserRoles)
            .SingleAsync(user => user.Id == SystemContentOwner.Id);
        Assert.Multiple(
            () => Assert.False(system.IsActive),
            () => Assert.Null(system.DeletedAt),
            () => Assert.Null(system.Email),
            () => Assert.False(system.HasPassword),
            () => Assert.Empty(system.Password),
            () => Assert.Null(system.GoogleSubject),
            () => Assert.Null(system.TelegramUserId),
            () => Assert.Empty(system.UserRoles));
        Recipe root = await observer.Recipes.AsNoTracking().Include(recipe => recipe.Images)
            .Include(recipe => recipe.Steps).ThenInclude(step => step.Images)
            .SingleAsync(recipe => recipe.Id == scenario.Root.Id);
        Recipe child = await observer.Recipes.AsNoTracking().SingleAsync(recipe => recipe.Id == scenario.Child.Id);
        Assert.Multiple(
            () => Assert.Equal(SystemContentOwner.Id, root.UserId),
            () => Assert.Equal(scenario.Root.Visibility, root.Visibility),
            () => Assert.Equal(SystemContentOwner.Id, child.UserId),
            () => Assert.Equal(Visibility.Private, child.Visibility),
            () => Assert.Equal(scenario.Root.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId),
                root.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)),
            () => Assert.Equal(scenario.Root.Steps.Single().Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId),
                root.Steps.Single().Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)));
        Assert.False(await observer.Recipes.AnyAsync(recipe => recipe.Id == scenario.UnrelatedRecipe.Id));
        Assert.False(await observer.Products.AnyAsync(product => product.Id == scenario.UnrelatedProduct.Id));
        foreach (Product product in scenario.RetainedProducts) {
            Product retained = await observer.Products.AsNoTracking().SingleAsync(item => item.Id == product.Id);
            Assert.Multiple(
                () => Assert.Equal(SystemContentOwner.Id, retained.UserId),
                () => Assert.Equal(product.Visibility, retained.Visibility));
        }
        foreach (ImageAsset asset in scenario.RetainedAssets) {
            ImageAsset retained = await observer.ImageAssets.AsNoTracking().SingleAsync(item => item.Id == asset.Id);
            Assert.Multiple(
                () => Assert.Equal(SystemContentOwner.Id, retained.UserId),
                () => Assert.Equal(asset.Url, retained.Url),
                () => Assert.Equal(asset.ObjectKey, retained.ObjectKey));
        }
        Assert.False(await observer.ImageAssets.AnyAsync(asset => asset.Id == scenario.UnrelatedAsset.Id));
        Assert.Equal([scenario.UnrelatedAsset.ObjectKey], outbox.ObjectKeys);
        await AssertForeignSystemOwnershipReferencesAsync(observer, scenario);
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_RetentionOwnerFailure_RollsBackAllContentButKeepsPermanentPrincipal() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        SystemOwnershipScenario scenario = await SeedSystemOwnershipScenarioAsync(context, privateRoot: false);
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(),
            out UserCleanupService service, new FailingParticipant(context));

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(0, result.RemovedCount);
        Assert.False(context.ChangeTracker.HasChanges());
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.True(await observer.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));
        Assert.True(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        Assert.True(await observer.Recipes.AnyAsync(recipe => recipe.Id == scenario.UnrelatedRecipe.Id));
        Assert.True(await observer.Products.AnyAsync(product => product.Id == scenario.UnrelatedProduct.Id));
        Assert.All(await observer.Recipes.Where(recipe => recipe.Id == scenario.Root.Id || recipe.Id == scenario.Child.Id)
            .ToListAsync(), recipe => Assert.Equal(scenario.Deleted.Id, recipe.UserId));
        Assert.All(await observer.Products.Where(product => product.UserId != scenario.Survivor.Id).ToListAsync(),
            product => Assert.Equal(scenario.Deleted.Id, product.UserId));
        Assert.All(await observer.ImageAssets.ToListAsync(), asset => Assert.Equal(scenario.Deleted.Id, asset.UserId));
        await AssertForeignSystemOwnershipReferencesAsync(observer, scenario);
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_ValidConfiguredSuccessor_PreservesFullReassignmentWithoutSystemOwner() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        SystemOwnershipScenario scenario = await SeedSystemOwnershipScenarioAsync(context, privateRoot: false);
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, scenario.Survivor.Id.Value);

        Assert.Equal(1, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == SystemContentOwner.Id));
        Assert.False(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        Assert.All(await observer.Recipes.ToListAsync(), recipe => Assert.Equal(scenario.Survivor.Id, recipe.UserId));
        Assert.All(await observer.Products.ToListAsync(), product => Assert.Equal(scenario.Survivor.Id, product.UserId));
        Assert.All(await observer.ImageAssets.ToListAsync(), asset => Assert.Equal(scenario.Survivor.Id, asset.UserId));
        Assert.True(await observer.Recipes.AnyAsync(recipe => recipe.Id == scenario.UnrelatedRecipe.Id));
        Assert.True(await observer.Products.AnyAsync(product => product.Id == scenario.UnrelatedProduct.Id));
        await AssertForeignSystemOwnershipReferencesAsync(observer, scenario);
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_OnlyOwnReferences_DoesNotCreateSystemOwner() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deleted = User.Create("unreferenced-owner@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        Product product = CreateSystemOwnershipProduct(deleted.Id, "Own private product");
        var recipe = Recipe.Create(deleted.Id, "Own private recipe", 1, visibility: Visibility.Private);
        recipe.AddStep(1, "Owned dependency").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(1));
        var plan = MealPlan.CreateForUser(deleted.Id, "Own plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Dinner, recipe.Id, 1);
        context.AddRange(deleted, product, recipe, plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(1, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.Empty(await observer.Users.ToListAsync());
        Assert.Empty(await observer.Recipes.ToListAsync());
        Assert.Empty(await observer.Products.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_ConcurrentPurge_ProvisionsOneReservedOwnerAndRetainsBothGraphs() {
        await using FoodDiaryDbContext firstContext = await databaseFixture.CreateDbContextAsync();
        var first = User.Create("first-retention@example.test", "hash");
        var second = User.Create("second-retention@example.test", "hash");
        var survivor = User.Create("retention-consumer@example.test", "hash");
        first.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        second.MarkDeleted(DateTime.UtcNow.AddDays(-9));
        var firstRecipe = Recipe.Create(first.Id, "First retained", 1, visibility: Visibility.Public);
        var secondRecipe = Recipe.Create(second.Id, "Second retained", 1, visibility: Visibility.Public);
        var parent = Recipe.Create(survivor.Id, "Foreign consumer", 1);
        RecipeStep parentStep = parent.AddStep(1, "Both dependencies");
        parentStep.AddNestedRecipeIngredient(firstRecipe.Id, RecipeServingQuantity.FromServings(1));
        parentStep.AddNestedRecipeIngredient(secondRecipe.Id, RecipeServingQuantity.FromServings(1));
        firstContext.AddRange(first, second, survivor, firstRecipe, secondRecipe, parent);
        await firstContext.SaveChangesAsync();
        firstContext.ChangeTracker.Clear();
        await using FoodDiaryDbContext secondContext = CreateVerificationContext(firstContext);
        await using ServiceProvider firstProvider = CreateServiceProvider(firstContext, new RecordingImageObjectDeletionOutbox(), out UserCleanupService firstService);
        await using ServiceProvider secondProvider = CreateServiceProvider(secondContext, new RecordingImageObjectDeletionOutbox(), out UserCleanupService secondService);

        UserCleanupBatch[] results = await Task.WhenAll(
            firstService.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null),
            secondService.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null));

        Assert.Equal(2, results.Sum(result => result.RemovedCount));
        await using FoodDiaryDbContext observer = CreateVerificationContext(firstContext);
        Assert.Equal(1, await observer.Users.CountAsync(user => user.Id == SystemContentOwner.Id));
        Assert.False(await observer.Users.AnyAsync(user => user.Id == first.Id || user.Id == second.Id));
        Assert.All(await observer.Recipes.Where(recipe => recipe.Id == firstRecipe.Id || recipe.Id == secondRecipe.Id).ToListAsync(),
            recipe => Assert.Equal(SystemContentOwner.Id, recipe.UserId));
        Assert.Equal(2, await observer.RecipeIngredients.CountAsync());
    }

    [RequiresDockerFact]
    public async Task CleanupDeletedUsersAsync_ReservedIdentityCollision_DoesNotAdoptAnAuthenticatableAccount() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        SystemOwnershipScenario scenario = await SeedSystemOwnershipScenarioAsync(context, privateRoot: false);
        var occupied = User.CreateSystemContentOwner();
        context.Users.Add(occupied);
        // Simulate a legacy/corrupt reserved row without bypassing production factory guards.
        context.Entry(occupied).Property(user => user.IsActive).CurrentValue = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(0, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.True(await observer.Users.AnyAsync(user => user.Id == scenario.Deleted.Id));
        Assert.Equal(scenario.Deleted.Id, (await observer.Recipes.SingleAsync(recipe => recipe.Id == scenario.Root.Id)).UserId);
        Assert.True((await observer.Users.SingleAsync(user => user.Id == SystemContentOwner.Id)).IsActive);
    }

    [RequiresDockerTheory]
    [InlineData("nested")]
    [InlineData("meal")]
    [InlineData("plan")]
    [InlineData("global-plan")]
    [InlineData("favorite")]
    [InlineData("comment")]
    [InlineData("like")]
    public async Task CleanupDeletedUsersAsync_RetainsRecipeForEachSurvivingReference(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deleted = User.Create("single-reference-owner@example.test", "hash");
        var survivor = User.Create("single-reference-consumer@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        var recipe = Recipe.Create(deleted.Id, "Only referenced recipe", 1, visibility: Visibility.Public);
        context.AddRange(deleted, survivor, recipe);
        switch (reference) {
            case "nested":
                var parent = Recipe.Create(survivor.Id, "Only nested reference", 1);
                parent.AddStep(1, "Use source").AddNestedRecipeIngredient(recipe.Id, RecipeServingQuantity.FromServings(1));
                context.Recipes.Add(parent);
                break;
            case "meal":
                var meal = Meal.Create(survivor.Id, DateTime.UtcNow);
                meal.AddRecipe(recipe.Id, RecipeServingQuantity.FromServings(1));
                context.Meals.Add(meal);
                break;
            case "plan":
            case "global-plan":
                MealPlan plan = string.Equals(reference, "plan", StringComparison.Ordinal)
                    ? MealPlan.CreateForUser(survivor.Id, "Only plan reference", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null)
                    : MealPlan.CreateCurated("Only global plan reference", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
                plan.AddDay(1).AddMeal(MealType.Dinner, recipe.Id, 1);
                context.MealPlans.Add(plan);
                break;
            case "favorite":
                context.FavoriteRecipes.Add(FavoriteRecipe.Create(survivor.Id, recipe.Id));
                break;
            case "comment":
                context.RecipeComments.Add(RecipeComment.Create(survivor.Id, recipe.Id, "Only comment reference"));
                break;
            case "like":
                context.RecipeLikes.Add(RecipeLike.Create(survivor.Id, recipe.Id));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reference));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(1, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.Equal(SystemContentOwner.Id, (await observer.Recipes.SingleAsync(item => item.Id == recipe.Id)).UserId);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == deleted.Id));
        if (string.Equals(reference, "like", StringComparison.Ordinal)) {
            Assert.Equal(recipe.Id, (await observer.RecipeLikes.SingleAsync()).RecipeId);
        }
    }

    [RequiresDockerTheory]
    [InlineData("recipe")]
    [InlineData("meal")]
    [InlineData("favorite")]
    [InlineData("shopping")]
    public async Task CleanupDeletedUsersAsync_RetainsProductForEachSurvivingReference(string reference) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var deleted = User.Create("single-product-owner@example.test", "hash");
        var survivor = User.Create("single-product-consumer@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        Product product = CreateSystemOwnershipProduct(deleted.Id, "Only referenced product");
        context.AddRange(deleted, survivor, product);
        switch (reference) {
            case "recipe":
                var recipe = Recipe.Create(survivor.Id, "Only ingredient reference", 1);
                recipe.AddStep(1, "Use product").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(1));
                context.Recipes.Add(recipe);
                break;
            case "meal":
                var meal = Meal.Create(survivor.Id, DateTime.UtcNow);
                meal.AddProduct(product.Id, ProductUnitQuantity.FromUnits(1));
                context.Meals.Add(meal);
                break;
            case "favorite":
                context.FavoriteProducts.Add(FavoriteProduct.Create(survivor.Id, product.Id));
                break;
            case "shopping":
                var shopping = ShoppingList.Create(survivor.Id, "Only shopping reference");
                shopping.AddItem("Retained product", product.Id, 1, MeasurementUnit.G, category: null, isChecked: false, 0);
                context.ShoppingLists.Add(shopping);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reference));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateServiceProvider(context, new RecordingImageObjectDeletionOutbox(), out UserCleanupService service);

        UserCleanupBatch result = await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null);

        Assert.Equal(1, result.RemovedCount);
        await using FoodDiaryDbContext observer = CreateVerificationContext(context);
        Assert.Equal(SystemContentOwner.Id, (await observer.Products.SingleAsync(item => item.Id == product.Id)).UserId);
        Assert.False(await observer.Users.AnyAsync(user => user.Id == deleted.Id));
    }

    private static async Task<SystemOwnershipScenario> SeedSystemOwnershipScenarioAsync(FoodDiaryDbContext context, bool privateRoot) {
        var deleted = User.Create("retained-catalog-owner@example.test", "hash");
        var survivor = User.Create("retained-catalog-consumer@example.test", "hash");
        deleted.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        ImageAsset[] retainedAssets = [.. Enumerable.Range(0, 6).Select(index => ImageAsset.Create(deleted.Id,
            FormattableString.Invariant($"retained/{index}.webp"), FormattableString.Invariant($"https://cdn.example.test/retained/{index}.webp")))];
        var unrelatedAsset = ImageAsset.Create(deleted.Id, "purged/private.webp", "https://cdn.example.test/purged/private.webp");
        foreach (ImageAsset asset in retainedAssets) { asset.Confirm(); }
        unrelatedAsset.Confirm();
        Product dependency = CreateSystemOwnershipProduct(deleted.Id, "Private transitive ingredient");
        dependency.ReplaceImages([new ProductImage(retainedAssets[4].Id, retainedAssets[4].Url, 0),
            new ProductImage(retainedAssets[5].Id, retainedAssets[5].Url, 1)]);
        Product direct = CreateSystemOwnershipProduct(deleted.Id, "Direct meal product");
        Product shoppingProduct = CreateSystemOwnershipProduct(deleted.Id, "Shopping reference product");
        Product unrelatedProduct = CreateSystemOwnershipProduct(deleted.Id, "Unreferenced private product");
        var root = Recipe.Create(deleted.Id, "Referenced root", 1, visibility: privateRoot ? Visibility.Private : Visibility.Public);
        root.ReplaceImages([new RecipeImage(retainedAssets[0].Id, retainedAssets[0].Url, 0),
            new RecipeImage(retainedAssets[1].Id, retainedAssets[1].Url, 1)]);
        RecipeStep rootStep = root.AddStep(1, "Use the private nested dependency");
        rootStep.ReplaceImages([new RecipeImage(retainedAssets[2].Id, retainedAssets[2].Url, 0),
            new RecipeImage(retainedAssets[3].Id, retainedAssets[3].Url, 1)]);
        var child = Recipe.Create(deleted.Id, "Private transitive recipe", 1, visibility: Visibility.Private);
        RecipeStep childStep = child.AddStep(1, "Private ingredient");
        childStep.AddProductIngredient(dependency.Id, ProductUnitQuantity.FromUnits(1));
        if (privateRoot) {
            // Retained legacy graphs may contain a cycle; closure must terminate without dropping dependencies.
            childStep.AddNestedRecipeIngredient(root.Id, RecipeServingQuantity.FromServings(1));
        }
        rootStep.AddNestedRecipeIngredient(child.Id, RecipeServingQuantity.FromServings(1));
        var unrelatedRecipe = Recipe.Create(deleted.Id, "Unreferenced private recipe", 1,
            imageUrl: unrelatedAsset.Url, imageAssetId: unrelatedAsset.Id, visibility: Visibility.Private);
        var foreignRecipe = Recipe.Create(survivor.Id, "Foreign nested consumer", 1);
        foreignRecipe.AddStep(1, "Retain source").AddNestedRecipeIngredient(root.Id, RecipeServingQuantity.FromServings(1));
        var foreignMeal = Meal.Create(survivor.Id, DateTime.UtcNow);
        foreignMeal.AddRecipe(root.Id, RecipeServingQuantity.FromServings(1));
        foreignMeal.AddProduct(direct.Id, ProductUnitQuantity.FromUnits(1));
        var foreignPlan = MealPlan.CreateForUser(survivor.Id, "Foreign plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        foreignPlan.AddDay(1).AddMeal(MealType.Dinner, root.Id, 1);
        var favorite = FavoriteRecipe.Create(survivor.Id, root.Id, "Keep favorite");
        var productFavorite = FavoriteProduct.Create(survivor.Id, direct.Id, "Keep product favorite");
        var comment = RecipeComment.Create(survivor.Id, root.Id, "Keep foreign comment");
        var shopping = ShoppingList.Create(survivor.Id, "Keep shopping reference");
        shopping.AddItem("Retained product", shoppingProduct.Id, 1, MeasurementUnit.G, category: null, isChecked: false, 0);
        context.AddRange(deleted, survivor, dependency, direct, shoppingProduct, unrelatedProduct, root, child, unrelatedRecipe,
            foreignRecipe, foreignMeal, foreignPlan, favorite, productFavorite, comment, shopping, unrelatedAsset);
        context.ImageAssets.AddRange(retainedAssets);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new SystemOwnershipScenario(deleted, survivor, root, child, unrelatedRecipe, unrelatedProduct,
            [dependency, direct, shoppingProduct], retainedAssets, unrelatedAsset, foreignRecipe, foreignMeal, foreignPlan,
            favorite, productFavorite, comment, shopping);
    }

    private static Product CreateSystemOwnershipProduct(UserId owner, string name) =>
        Product.Create(owner, name, MeasurementUnit.G, 100, defaultPortionAmount: null, 100, 1, 1, 1, 0, 0, visibility: Visibility.Private);

    private static async Task AssertForeignSystemOwnershipReferencesAsync(FoodDiaryDbContext observer, SystemOwnershipScenario scenario) {
        RecipeIngredient nested = await observer.RecipeIngredients.AsNoTracking()
            .SingleAsync(item => item.RecipeStep.RecipeId == scenario.ForeignRecipe.Id);
        Meal meal = await observer.Meals.AsNoTracking().Include(item => item.Items).SingleAsync(item => item.Id == scenario.ForeignMeal.Id);
        MealPlan plan = await observer.MealPlans.AsNoTracking().Include(item => item.Days).ThenInclude(day => day.Meals)
            .SingleAsync(item => item.Id == scenario.ForeignPlan.Id);
        ShoppingList shopping = await observer.ShoppingLists.AsNoTracking().Include(item => item.Items)
            .SingleAsync(item => item.Id == scenario.Shopping.Id);
        Assert.Multiple(
            () => Assert.Equal(scenario.Root.Id, nested.NestedRecipeId),
            () => Assert.Equal(scenario.Root.Id, Assert.Single(meal.Items, item => item.RecipeId is not null).RecipeId),
            () => Assert.Equal(scenario.RetainedProducts[1].Id, Assert.Single(meal.Items, item => item.ProductId is not null).ProductId),
            () => Assert.Equal(scenario.Root.Id, Assert.Single(Assert.Single(plan.Days).Meals).RecipeId),
            () => Assert.Equal(scenario.RetainedProducts[2].Id, Assert.Single(shopping.Items).ProductId),
            () => Assert.Equal(scenario.Survivor.Id, meal.UserId),
            () => Assert.Equal(scenario.Survivor.Id, plan.UserId),
            () => Assert.Equal(scenario.Survivor.Id, shopping.UserId));
        Assert.Equal(scenario.Root.Id, (await observer.FavoriteRecipes.AsNoTracking().SingleAsync()).RecipeId);
        Assert.Equal(scenario.RetainedProducts[1].Id, (await observer.FavoriteProducts.AsNoTracking().SingleAsync()).ProductId);
        Assert.Equal(scenario.Comment.Text, (await observer.RecipeComments.AsNoTracking().SingleAsync()).Text);
    }

    [ExcludeFromCodeCoverage]
    private sealed record SystemOwnershipScenario(User Deleted, User Survivor, Recipe Root, Recipe Child,
        Recipe UnrelatedRecipe, Product UnrelatedProduct, IReadOnlyList<Product> RetainedProducts,
        IReadOnlyList<ImageAsset> RetainedAssets, ImageAsset UnrelatedAsset, Recipe ForeignRecipe, Meal ForeignMeal,
        MealPlan ForeignPlan, FavoriteRecipe Favorite, FavoriteProduct ProductFavorite, RecipeComment Comment, ShoppingList Shopping);
}
