using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.MealPlanning;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetMealPlanById;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MealPlanRecipePrivacyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RecipeSnapshots_RejectForeignPrivateRootsButPreservePublicAndOwnedPrivateRecipes() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var author = User.Create($"plan-author-{Guid.NewGuid():N}@example.com", "hash");
        var viewer = User.Create($"plan-viewer-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(author.Id, "Public ingredient", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        product.ChangeVisibility(Visibility.Public);
        var hidden = Recipe.Create(author.Id, "New private content", 1, visibility: Visibility.Private);
        hidden.AddStep(1, "Secret combination").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(777));
        var published = Recipe.Create(author.Id, "Published", 1, visibility: Visibility.Public);
        var owned = Recipe.Create(viewer.Id, "Own private", 1, visibility: Visibility.Private);
        context.AddRange(author, viewer, product, hidden, published, owned);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new MealPlanCompositionReader(context);

        RecipeId[] ids = [hidden.Id, published.Id, owned.Id];
        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> result = await reader.GetRecipeSnapshotsAsync(ids, viewer.Id);
        Assert.Multiple(
            () => Assert.DoesNotContain(hidden.Id, result.Keys),
            () => Assert.Contains(published.Id, result.Keys),
            () => Assert.Contains(owned.Id, result.Keys));
        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> anonymous = await reader.GetRecipeSnapshotsAsync(ids, viewerId: null);
        Assert.Equal(published.Id, Assert.Single(anonymous).Key);
        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> authorResult = await reader.GetRecipeSnapshotsAsync(ids, author.Id);
        Assert.Equal(777, Assert.Single(authorResult[hidden.Id].Ingredients).Amount);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task LegacyPrivateReferences_AreHiddenInCatalogAndAdoptedPlanDetailsAndShopping() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var author = User.Create($"private-plan-author-{Guid.NewGuid():N}@example.com", "hash");
        var viewer = User.Create($"private-plan-viewer-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(author.Id, "Public ingredient", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        product.ChangeVisibility(Visibility.Public);
        var hidden = Recipe.Create(author.Id, "Secret name", 1, visibility: Visibility.Private);
        hidden.AddStep(1, "Secret combination").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(777));
        var published = Recipe.Create(author.Id, "Published", 1, visibility: Visibility.Public);
        published.AddStep(1, "Mix").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(20));
        var owned = Recipe.Create(viewer.Id, "Own private", 1, visibility: Visibility.Private);
        owned.AddStep(1, "Mix").AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(50));
        var catalog = MealPlan.CreateCurated("Legacy", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        MealPlanDay day = catalog.AddDay(1);
        day.AddMeal(MealType.Breakfast, hidden.Id, 1);
        day.AddMeal(MealType.Lunch, published.Id, 1);
        day.AddMeal(MealType.Dinner, owned.Id, 1);
        MealPlan adopted = catalog.Adopt(viewer.Id);
        context.AddRange(author, viewer, product, hidden, published, owned, catalog, adopted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context);
        IMealPlanCompositionReader reader = provider.GetRequiredService<IMealPlanCompositionReader>();
        IMealPlanReadModelRepository reads = provider.GetRequiredService<IMealPlanReadModelRepository>();
        var access = new OwnedUserAccess(viewer.Id);
        var detail = new GetMealPlanByIdQueryHandler(reads, access);

        MealPlanReadModel anonymous = (await reader.GetReadModelByIdAsync(catalog.Id, viewerId: null))!;
        Assert.Equal(published.Id.Value, Assert.Single(Assert.Single(anonymous.Days).Meals).RecipeId);
        Result<MealPlanModel> admin = await new GetCatalogMealPlanQueryHandler(reader)
            .Handle(new GetCatalogMealPlanQuery(catalog.Id.Value), CancellationToken.None);
        ResultAssert.Success(admin);
        Assert.Equal(published.Id.Value, Assert.Single(Assert.Single(admin.Value.Days).Meals).RecipeId);
        foreach (MealPlan plan in new[] { catalog, adopted }) {
            Result<MealPlanModel> result = await detail.Handle(new GetMealPlanByIdQuery(viewer.Id.Value, plan.Id.Value), CancellationToken.None);
            ResultAssert.Success(result);
            Assert.Equal(new[] { published.Id.Value, owned.Id.Value }.Order(), Assert.Single(result.Value.Days).Meals.Select(meal => meal.RecipeId).Order());
            Assert.Empty(context.ChangeTracker.Entries());

            var shopping = new GenerateShoppingListCommandHandler(provider.GetRequiredService<IMealPlanReadRepository>(),
                provider.GetRequiredService<IShoppingListCreationService>(), access);
            Result<ShoppingListModel> generated = await shopping.Handle(
                new GenerateShoppingListCommand(viewer.Id.Value, plan.Id.Value), CancellationToken.None);
            ResultAssert.Success(generated);
            ShoppingListItemModel item = Assert.Single(generated.Value.Items);
            Assert.Multiple(
                () => Assert.Equal(70, item.Amount),
                () => Assert.Equal(product.Id.Value, item.ProductId),
                () => Assert.Equal(new[] { published.Id.Value, owned.Id.Value }.Order(), item.Sources.Select(source => source.RecipeId!.Value).Order()),
                () => Assert.DoesNotContain(item.Sources, source => source.RecipeId == hidden.Id.Value || string.Equals(source.Label, hidden.Name, StringComparison.Ordinal)));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
    }

    [RequiresDockerFact]
    public Task RecipeEditWinsAfterPublicCheck_CatalogRetriesAndRejectsPrivateRecipe() =>
        AssertRaceAsync(recipeEditWins: true);

    [RequiresDockerFact]
    public async Task TextIngredients_PreserveLiteralNotesInGeneratedShoppingAndRespectRecipeVisibility() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var author = User.Create($"text-plan-author-{Guid.NewGuid():N}@example.com", "hash");
        var viewer = User.Create($"text-plan-viewer-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(author.Id, "Public rice", MeasurementUnit.G, 100, 100, 120, 3, 1, 20, 2, 0);
        product.ChangeVisibility(Visibility.Public);
        var published = Recipe.Create(author.Id, "Text and measured ingredients", 2, visibility: Visibility.Public);
        RecipeStep step = published.AddStep(1, "Mix");
        step.AddProductIngredient(product.Id, ProductUnitQuantity.FromUnits(100));
        step.AddTextIngredient("Salt", "to taste");
        step.AddTextIngredient("Water", "100 g");
        step.AddTextIngredient("Garnish", amountText: null);
        var hidden = Recipe.Create(author.Id, "Private text recipe", 1, visibility: Visibility.Private);
        hidden.AddStep(1, "Secret").AddTextIngredient("Secret ingredient", "private quantity");
        var plan = MealPlan.CreateForUser(viewer.Id, "Literal quantities", description: null, DietType.Balanced, 2, targetCaloriesPerDay: null);
        MealPlanDay first = plan.AddDay(1);
        first.AddMeal(MealType.Lunch, published.Id, 2);
        first.AddMeal(MealType.Breakfast, hidden.Id, 1);
        plan.AddDay(2).AddMeal(MealType.Dinner, published.Id, 1);
        context.AddRange(author, viewer, product, published, hidden, plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context);
        IMealPlanCompositionReader reader = provider.GetRequiredService<IMealPlanCompositionReader>();

        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> snapshots = await reader.GetRecipeSnapshotsAsync([published.Id, hidden.Id], viewer.Id);
        MealPlanRecipeSnapshot snapshot = Assert.Single(snapshots).Value;
        Assert.Equal(4, snapshot.Ingredients.Count);
        Assert.Empty(context.ChangeTracker.Entries());
        var handler = new GenerateShoppingListCommandHandler(provider.GetRequiredService<IMealPlanReadRepository>(),
            provider.GetRequiredService<IShoppingListCreationService>(), new OwnedUserAccess(viewer.Id));
        Result<ShoppingListModel> generated = await handler.Handle(
            new GenerateShoppingListCommand(viewer.Id.Value, plan.Id.Value), CancellationToken.None);

        ResultAssert.Success(generated);
        Assert.Equal(7, generated.Value.Items.Count);
        ShoppingListItemModel measured = Assert.Single(generated.Value.Items, item => item.ProductId == product.Id.Value);
        Assert.Multiple(
            () => Assert.Equal(150, measured.Amount),
            () => Assert.Equal(nameof(MeasurementUnit.G), measured.Unit),
            () => Assert.Equal(2, measured.Sources.Count));
        foreach ((string name, string? note) in new[] { ("Salt", "to taste"), ("Water", "100 g"), ("Garnish", (string?)null) }) {
            ShoppingListItemModel[] textItems = [.. generated.Value.Items.Where(item => string.Equals(item.Name, name, StringComparison.Ordinal))];
            Assert.Equal(2, textItems.Length);
            Assert.All(textItems, item => Assert.Multiple(
                () => Assert.Null(item.ProductId),
                () => Assert.Null(item.Amount),
                () => Assert.Null(item.Unit),
                () => Assert.Equal(note, item.Note),
                () => Assert.Empty(item.Sources)));
        }
        Assert.DoesNotContain(generated.Value.Items, item => string.Equals(item.Name, "Secret ingredient", StringComparison.Ordinal));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        context.ChangeTracker.Clear();
        var listId = new ShoppingListId(generated.Value.Id);
        var persisted = await context.ShoppingListItems.AsNoTracking()
            .Where(item => item.ShoppingListId == listId)
            .Select(item => new { item.Name, item.Amount, item.Unit, item.Note })
            .ToListAsync();
        Assert.Equal(7, persisted.Count);
        Assert.Equal(2, persisted.Count(item => string.Equals(item.Name, "Water", StringComparison.Ordinal) &&
            string.Equals(item.Note, "100 g", StringComparison.Ordinal) && item.Amount is null && item.Unit is null));
    }

    [RequiresDockerFact]
    public Task CatalogSaveWins_OverlappingRecipeEditRechecksUsageAndRejectsMutation() =>
        AssertRaceAsync(recipeEditWins: false);

    private async Task AssertRaceAsync(bool recipeEditWins) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var author = User.Create($"race-author-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(author.Id, "Before", 1, visibility: Visibility.Public);
        seed.AddRange(author, recipe);
        await seed.SaveChangesAsync();
        string connectionString = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext catalogContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        await using FoodDiaryDbContext editingContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        await using ServiceProvider catalogProvider = CreateProvider(catalogContext);
        await using ServiceProvider editingProvider = CreateProvider(editingContext);
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task PauseAsync(CancellationToken token) {
            paused.TrySetResult();
            await resume.Task.WaitAsync(TimeSpan.FromSeconds(45), token);
        }
        var catalogRecipes = new GatedCatalogRecipes(catalogProvider.GetRequiredService<IMealPlanCatalogRecipeReader>(),
            recipeEditWins ? PauseAsync : null);
        var catalogHandler = new SaveCatalogMealPlanCommandHandler(catalogProvider.GetRequiredService<IMealPlanCatalogRepository>(),
            catalogProvider.GetRequiredService<IMealPlanWriteRepository>(), catalogRecipes,
            catalogProvider.GetRequiredService<IMealPlanCompositionReader>(), catalogProvider.GetRequiredService<IMealPlanCatalogTransactionRunner>());
        IRecipeMutationTransactionRunner editRunner = editingProvider.GetRequiredService<IRecipeMutationTransactionRunner>();
        var gatedEdit = new GatedRecipeTransaction(editRunner, async token => {
            // Pin the owner's snapshot before taking the recipe row lock, allowing the catalog to commit first.
            Assert.True(await editingContext.Users.AsNoTracking().AnyAsync(item => item.Id == author.Id, token));
            await PauseAsync(token);
        });
        if (!recipeEditWins) { editRunner = gatedEdit; }
        IImageAssetAccessService imageAccess = Substitute.For<IImageAssetAccessService>();
        imageAccess.ResolveOptionalAsync(assetId: null, author.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<ImageAssetReadModel?>(value: null));
        var editHandler = new UpdateRecipeCommandHandler(editingProvider.GetRequiredService<IRecipeReadRepository>(),
            editingProvider.GetRequiredService<IRecipeWriteRepository>(), editingProvider.GetRequiredService<IRecipeNutritionWriter>(),
            Substitute.For<IImageAssetCleanupService>(), new OwnedUserAccess(author.Id), imageAccess,
            Substitute.For<IProductLookupService>(), Substitute.For<IRecipeLookupService>(), editRunner);
        var save = new SaveCatalogMealPlanCommand(Id: null, "Race", Description: null, "Balanced", 1, TargetCaloriesPerDay: null,
            IsPublished: true, [new CatalogDayInput(1, [new CatalogMealInput("Lunch", recipe.Id.Value, 1)])]);
        var edit = new UpdateRecipeCommand(author.Id.Value, recipe.Id.Value, "After private", Description: null, ClearDescription: false,
            Comment: null, ClearComment: false, Category: null, ClearCategory: false, ImageUrl: null, ClearImageUrl: false,
            ImageAssetId: null, ClearImageAssetId: false, PrepTime: null, CookTime: null, Servings: null, "Private",
            CalculateNutritionAutomatically: false, ManualCalories: 999, ManualProteins: 1, ManualFats: 1, ManualCarbs: 1,
            ManualFiber: 0, ManualAlcohol: 0, Steps: []);
        Task<Result<MealPlanModel>> saving;
        Task<Result<RecipeModel>> editing;
        if (recipeEditWins) {
            saving = catalogHandler.Handle(save, CancellationToken.None);
            await paused.Task.WaitAsync(TimeSpan.FromSeconds(45));
            editing = editHandler.Handle(edit, CancellationToken.None);
            await editing.WaitAsync(TimeSpan.FromSeconds(45));
            resume.TrySetResult();
        } else {
            editing = editHandler.Handle(edit, CancellationToken.None);
            await paused.Task.WaitAsync(TimeSpan.FromSeconds(45));
            saving = catalogHandler.Handle(save, CancellationToken.None);
            await saving.WaitAsync(TimeSpan.FromSeconds(45));
            resume.TrySetResult();
        }
        await Task.WhenAll(saving, editing).WaitAsync(TimeSpan.FromSeconds(60));
        Result<MealPlanModel> saved = await saving;
        Result<RecipeModel> edited = await editing;
        Recipe persisted = await seed.Recipes.AsNoTracking().SingleAsync(item => item.Id == recipe.Id);
        if (recipeEditWins) {
            ResultAssert.Success(edited);
            ResultAssert.Failure(saved);
            Assert.Multiple(
                () => Assert.Equal("MealPlan.RecipeUnavailable", saved.Error.Code),
                () => Assert.Equal(Visibility.Private, persisted.Visibility),
                () => Assert.Equal("After private", persisted.Name),
                () => Assert.True(catalogRecipes.Reads >= 2, "Catalog must retry its public-recipe check."));
            Assert.False(await seed.MealPlans.AsNoTracking().AnyAsync());
            Assert.False(await seed.MealPlanMeals.AsNoTracking().AnyAsync());
        } else {
            ResultAssert.Success(saved);
            ResultAssert.Failure(edited);
            Assert.Multiple(
                () => Assert.Contains("already used", edited.Error.Message, StringComparison.Ordinal),
                () => Assert.Equal(Visibility.Public, persisted.Visibility),
                () => Assert.Equal("Before", persisted.Name),
                () => Assert.True(gatedEdit.Attempts >= 2, "Recipe mutation must retry its stale snapshot and recheck usage."));
            Assert.Equal(recipe.Id, (await seed.MealPlanMeals.AsNoTracking().SingleAsync()).RecipeId);
        }
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build())
            .AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddSingleton<IProductSnapshotReadService>(new ProductSnapshotReadService(context.Products));
        services.AddReadModelComposition();
        services.AddRecipesPersistence();
        services.AddMealPlanningModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class GatedCatalogRecipes(IMealPlanCatalogRecipeReader inner, Func<CancellationToken, Task>? afterFirstRead) : IMealPlanCatalogRecipeReader {
        public int Reads { get; private set; }
        public Task<IReadOnlyList<CatalogRecipeReadModel>> SearchAsync(string? search, int limit, CancellationToken cancellationToken) =>
            inner.SearchAsync(search, limit, cancellationToken);

        public async Task<IReadOnlySet<Guid>> GetPublicIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) {
            IReadOnlySet<Guid> result = await inner.GetPublicIdsAsync(ids, cancellationToken);
            if (++Reads == 1 && afterFirstRead is not null) { await afterFirstRead(cancellationToken); }
            return result;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class GatedRecipeTransaction(IRecipeMutationTransactionRunner inner, Func<CancellationToken, Task> beforeRead) : IRecipeMutationTransactionRunner {
        public int Attempts { get; private set; }

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            inner.ExecuteAsync(async token => {
                Attempts++;
                await beforeRead(token);
                return await operation(token);
            }, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class OwnedUserAccess(UserId owner) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Assert.Equal(owner, userId);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Error?>(null);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
