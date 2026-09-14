using FoodDiary.Modules.RecipeCommunity.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class CommunityPlanningContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelsContainOnlyOwnedEntitiesAndExistingTableNames() {
        using var social = new RecipeCommunityDbContext(new DbContextOptionsBuilder<RecipeCommunityDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        using var planning = new MealPlanningDbContext(new DbContextOptionsBuilder<MealPlanningDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Type[] socialTypes = [typeof(RecipeComment), typeof(RecipeLike)];
        Type[] planningTypes = [typeof(MealPlan), typeof(MealPlanDay), typeof(MealPlanMeal), typeof(ShoppingList), typeof(ShoppingListItem), typeof(ShoppingListItemSource)];
        Assert.Equal(socialTypes.OrderBy(type => type.Name, StringComparer.Ordinal), social.Model.GetEntityTypes().Select(type => type.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.Equal(planningTypes.OrderBy(type => type.Name, StringComparer.Ordinal), planning.Model.GetEntityTypes().Select(type => type.ClrType).OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.All(social.Model.GetEntityTypes().Concat(planning.Model.GetEntityTypes()), entity => Assert.Equal(entity.ClrType.Name + "s", entity.GetTableName()));
    }

    [RequiresDockerFact]
    public async Task SharedSavePreservesNestedPlanningAndComposedReadsAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        RecipeCommunityDbContext social = provider.GetRequiredService<RecipeCommunityDbContext>();
        MealPlanningDbContext planning = provider.GetRequiredService<MealPlanningDbContext>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"planning-context-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Recipe", servings: 2);
        central.Users.Add(user);
        central.Recipes.Add(recipe);
        await provider.GetRequiredService<IRecipeLikeWriteRepository>().AddAsync(RecipeLike.Create(user.Id, recipe.Id));
        var plan = MealPlan.CreateForUser(user.Id, "Plan", description: null, DietType.Balanced, durationDays: 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Breakfast, recipe.Id, servings: 1);
        await provider.GetRequiredService<IMealPlanWriteRepository>().AddAsync(plan);
        var list = ShoppingList.Create(user.Id, "List");
        list.AddItem("Item", productId: null, amount: 1, MeasurementUnit.G, category: null, isChecked: false, sortOrder: 0);
        await provider.GetRequiredService<IShoppingListWriteRepository>().AddAsync(list);
        Assert.Empty(central.ChangeTracker.Entries<MealPlan>());
        Assert.Empty(central.ChangeTracker.Entries<RecipeLike>());
        Assert.Same(central.Database.GetDbConnection(), social.Database.GetDbConnection());
        Assert.Same(central.Database.GetDbConnection(), planning.Database.GetDbConnection());
        await unitOfWork.SaveChangesAsync();
        Assert.NotNull(await provider.GetRequiredService<IMealPlanReadModelRepository>().GetReadModelByIdAsync(plan.Id));
        planning.ChangeTracker.Clear();
        IShoppingListReadRepository reads = provider.GetRequiredService<IShoppingListReadRepository>();
        ShoppingList? loaded = await reads.GetByIdAsync(list.Id, user.Id, includeItems: true, asTracking: true);
        Assert.NotNull(loaded);
        loaded.UpdateName("Updated");
        Assert.Single(loaded.Items).UpdateDetails("Updated item", productId: null, amount: 2, MeasurementUnit.G,
            category: null, aisle: null, note: null, isChecked: false, checkedOnUtc: null, sortOrder: 0);
        await provider.GetRequiredService<IShoppingListWriteRepository>().UpdateAsync(loaded);
        await unitOfWork.SaveChangesAsync();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.Equal("Updated", (await read.ShoppingLists.SingleAsync(item => item.Id == list.Id)).Name);
        Assert.Equal("Updated item", (await read.ShoppingListItems.SingleAsync(item => item.ShoppingListId == list.Id)).Name);
        await new MealPlanningUserDataPurgeParticipant(planning,
            provider.GetRequiredService<FoodDiary.Persistence.Abstractions.IModuleTransactionCoordinator>())
            .PurgeAsync(user.Id, reassignTarget: null, CancellationToken.None);
        Assert.False(await read.ShoppingListItems.AnyAsync(item => item.ShoppingListId == list.Id));
        planning.MealPlans.Remove(await planning.MealPlans.SingleAsync(item => item.Id == plan.Id));
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.MealPlanMeals.AnyAsync(item => item.RecipeId == recipe.Id));
        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        Assert.False(await read.RecipeLikes.AnyAsync(item => item.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task DuplicateLikeRollsBackPreviouslySavedPlanningAndUserAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        MealPlanningDbContext planning = provider.GetRequiredService<MealPlanningDbContext>();
        RecipeCommunityDbContext social = provider.GetRequiredService<RecipeCommunityDbContext>();
        var user = User.Create($"planning-rollback-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "Recipe", servings: 1);
        central.Users.Add(user);
        central.Recipes.Add(recipe);
        planning.ShoppingLists.Add(ShoppingList.Create(user.Id, "Rollback"));
        social.RecipeLikes.AddRange(RecipeLike.Create(user.Id, recipe.Id), RecipeLike.Create(user.Id, recipe.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.ShoppingLists.AnyAsync(item => item.UserId == user.Id));
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddReadModelComposition();
        services.AddRecipeCommunityModule();
        services.AddMealPlanningModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
