using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Domain.Primitives;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.ShoppingLists;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ShoppingListRevisionPersistenceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task CreationAndTimestampRevisionsMatchRealPostgresRoundTripsAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"shopping-revision-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(owner);
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IShoppingListCreationService creation = provider.GetRequiredService<IShoppingListCreationService>();
        Result<ShoppingListModel> created = await creation.CreateAsync(new ShoppingListCreationRequest(owner.Id, "Generated with sources", [
            new ShoppingListCreationItem(ProductId: null, "Rice", 1.23456789012345, MeasurementUnit.G, "Grain", 0, [
                new ShoppingListCreationSource(MealPlanId.New(), MealPlanMealId.New(), RecipeId.New(), "Breakfast", 1, "Breakfast", 1.23456789012345, MeasurementUnit.G),
                new ShoppingListCreationSource(MealPlanId.New(), MealPlanMealId.New(), RecipeId.New(), "Breakfast", 1, "Breakfast", 3, MeasurementUnit.G),
            ]),
        ]), CancellationToken.None);
        ResultAssert.Success(created);
        string preCommitRevision = created.Value.Revision;
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        var repository = new ShoppingListRepository(observer.ShoppingLists);
        var listId = new ShoppingListId(created.Value.Id);
        ShoppingListReadModel? persisted = await repository.GetReadModelByIdAsync(listId, owner.Id);
        Assert.NotNull(persisted);
        Assert.Equal(preCommitRevision, persisted.ToModel().Revision);

        DateTime epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime[] timestamps = [epoch.AddTicks(1), epoch.AddTicks(9), epoch.AddTicks(-1), epoch.AddTicks(-9),
            new DateTime(2026, 10, 22, 10, 30, 0, DateTimeKind.Utc).AddTicks(1234567),
            DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)];
        await using var connection = new NpgsqlConnection(central.Database.GetConnectionString());
        await connection.OpenAsync();
        foreach (DateTime timestamp in timestamps) {
            await using var command = new NpgsqlCommand("SELECT @timestamp::timestamptz", connection);
            command.Parameters.AddWithValue("timestamp", NpgsqlDbType.TimestampTz, timestamp);
            object? actual = await command.ExecuteScalarAsync();
            DateTime roundTripped = Assert.IsType<DateTime>(actual);
            Assert.Equal(roundTripped.Ticks, ShoppingListRevision.NormalizeTimestamp(timestamp).Ticks);

            var list = ShoppingList.Create(owner.Id, "Timestamp fixture");
            list.AddItem("Checked", productId: null, 1, MeasurementUnit.Pcs, category: null, isChecked: true, 0, checkedOnUtc: timestamp);
            central.ShoppingLists.Add(list);
            central.Entry(list).Property(value => value.CreatedOnUtc).CurrentValue = timestamp;
            ShoppingListModel beforeCommit = list.ToModel();
            await central.SaveChangesAsync();
            ShoppingListReadModel? read = await repository.GetReadModelByIdAsync(list.Id, owner.Id);
            Assert.NotNull(read);
            Assert.Equal(beforeCommit.Revision, read.ToModel().Revision);
            Assert.Equal(beforeCommit.CreatedAt, read.CreatedAt);
            Assert.Equal(Assert.Single(beforeCommit.Items).CheckedOnUtc, Assert.Single(read.Items).CheckedOnUtc);
        }
    }

    [RequiresDockerFact]
    public async Task GenerateHandlerResponseRevisionMatchesPersistedDetailAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"shopping-generate-revision-{Guid.NewGuid():N}@example.com", "hash");
        var recipe = Recipe.Create(owner.Id, "Salt recipe", 1, visibility: Visibility.Private);
        recipe.AddStep(1, "Season").AddTextIngredient("Salt", "to taste");
        var plan = MealPlan.CreateForUser(owner.Id, "Plan", description: null, DietType.Balanced, 1, targetCaloriesPerDay: null);
        plan.AddDay(1).AddMeal(MealType.Dinner, recipe.Id, 1);
        central.AddRange(owner, recipe, plan);
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var handler = new GenerateShoppingListCommandHandler(provider.GetRequiredService<IMealPlanReadRepository>(),
            provider.GetRequiredService<IShoppingListCreationService>(), new OwnerAccess(owner.Id));
        Result<ShoppingListModel> generated = await handler.Handle(new GenerateShoppingListCommand(owner.Id.Value, plan.Id.Value), CancellationToken.None);
        ResultAssert.Success(generated);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        ShoppingListReadModel? persisted = await new ShoppingListRepository(observer.ShoppingLists)
            .GetReadModelByIdAsync(new ShoppingListId(generated.Value.Id), owner.Id);
        Assert.NotNull(persisted);
        Assert.Equal(generated.Value.Revision, persisted.ToModel().Revision);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SameSnapshotChildWritesUseParentXminAndRollBackAllLosingChangesAsync(bool replaceItems) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"shopping-cas-{Guid.NewGuid():N}@example.com", "hash");
        var list = ShoppingList.Create(owner.Id, "Unchanged name");
        list.AddItem("Original", productId: null, 1, MeasurementUnit.Pcs, category: null, isChecked: false, 0);
        seed.AddRange(owner, list);
        await seed.SaveChangesAsync();
        string connection = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connection);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connection);
        await using ServiceProvider firstProvider = CreateProvider(first);
        await using ServiceProvider secondProvider = CreateProvider(second);
        MealPlanningDbContext firstPlanning = firstProvider.GetRequiredService<MealPlanningDbContext>();
        MealPlanningDbContext secondPlanning = secondProvider.GetRequiredService<MealPlanningDbContext>();
        ShoppingList firstList = await LoadTrackedAsync(firstProvider, list.Id, owner.Id);
        ShoppingList secondList = await LoadTrackedAsync(secondProvider, list.Id, owner.Id);
        Assert.Equal(firstList.ToModel().Revision, secondList.ToModel().Revision);
        uint originalXmin = firstPlanning.Entry(firstList).Property<uint>("xmin").CurrentValue;
        Assert.Equal(originalXmin, secondPlanning.Entry(secondList).Property<uint>("xmin").CurrentValue);
        await StageChildrenAsync(firstProvider, firstList, "A", replaceItems);
        await StageChildrenAsync(secondProvider, secondList, "B", replaceItems);
        var firstSideEffect = User.Create($"shopping-side-a-{Guid.NewGuid():N}@example.com", "hash");
        var secondSideEffect = User.Create($"shopping-side-b-{Guid.NewGuid():N}@example.com", "hash");
        first.Users.Add(firstSideEffect);
        second.Users.Add(secondSideEffect);

        // Both snapshots and all child changes are staged before either commit.
        Exception?[] outcomes = await Task.WhenAll(
            Record.ExceptionAsync(() => firstProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()),
            Record.ExceptionAsync(() => secondProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()));

        Assert.Single(outcomes, exception => exception is null);
        Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(outcomes.OfType<Exception>()));
        string winner = outcomes[0] is null ? "A" : "B";
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(connection);
        ShoppingList committed = await observer.ShoppingLists.Include(value => value.Items).SingleAsync(value => value.Id == list.Id);
        Assert.Multiple(
            () => Assert.Equal("Unchanged name", committed.Name),
            () => Assert.Equal(replaceItems ? 2 : 1, committed.Items.Count),
            () => Assert.All(committed.Items, item => Assert.StartsWith(winner, item.Name, StringComparison.Ordinal)));
        Assert.NotEqual(originalXmin, observer.Entry(committed).Property<uint>("xmin").CurrentValue);
        Assert.Equal(outcomes[0] is null, await observer.Users.AnyAsync(user => user.Id == firstSideEffect.Id));
        Assert.Equal(outcomes[1] is null, await observer.Users.AnyAsync(user => user.Id == secondSideEffect.Id));
    }

    private static async Task<ShoppingList> LoadTrackedAsync(ServiceProvider provider, ShoppingListId id, UserId owner) {
        ShoppingList? list = await provider.GetRequiredService<IShoppingListReadRepository>()
            .GetByIdAsync(id, owner, includeItems: true, asTracking: true);
        Assert.NotNull(list);
        return list;
    }

    private static async Task StageChildrenAsync(ServiceProvider provider, ShoppingList list, string label, bool replaceItems) {
        DateTime? rootModifiedBeforeChildEdit = list.ModifiedOnUtc;
        if (replaceItems) {
            list.ClearItems();
            list.AddItem($"{label} one", productId: null, 1, MeasurementUnit.Pcs, category: null, isChecked: false, 0);
            list.AddItem($"{label} two", productId: null, 2, MeasurementUnit.Pcs, category: null, isChecked: false, 1);
        } else {
            Assert.Single(list.Items).UpdateDetails($"{label} edited", productId: null, 2, MeasurementUnit.Pcs, category: null, aisle: null,
                label, isChecked: true, checkedOnUtc: null, 0);
            Assert.Equal(rootModifiedBeforeChildEdit, list.ModifiedOnUtc);
        }
        await provider.GetRequiredService<IShoppingListWriteRepository>().UpdateAsync(list);
        Assert.Equal(EntityState.Modified, provider.GetRequiredService<MealPlanningDbContext>().Entry(list).State);
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build())
            .AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddReadModelComposition();
        services.AddMealPlanningModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class OwnerAccess(UserId owner) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Error?>(userId == owner ? null : throw new InvalidOperationException("Unexpected test owner."));
    }
}
