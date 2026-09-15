using FoodDiary.Persistence.Runtime;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using System.Data;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedRecipesContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSaveAndIntermediateMutationFlushUseOwnerContextAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        RecipesDbContext owned = provider.GetRequiredService<RecipesDbContext>();
        IRecipeRepository writes = provider.GetRequiredService<IRecipeRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create("recipe-owner@example.com", "hash");
        Recipe recipe = CreateRecipe(user);
        var ingredient = Product.Create(user.Id, "Ingredient", MeasurementUnit.G, 100, 100, 100, 10, 5, 10, 1, 0);
        var nested = Recipe.Create(user.Id, "Nested recipe", servings: 1);
        recipe.AddStep(1, "Mix").AddProductIngredient(ingredient.Id, 100);
        recipe.Steps.Single().AddNestedRecipeIngredient(nested.Id, 1);
        shared.AddRange(user, ingredient);
        await writes.AddAsync(nested);
        await writes.AddAsync(recipe);
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(3, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<Recipe>());
        Assert.Equal(2, await database.Recipes.CountAsync());
        uint createdVersion = owned.Entry(recipe).Property<uint>("xmin").CurrentValue;
        Assert.NotEqual(0u, createdVersion);
        await provider.GetRequiredService<IRecipeMutationTransactionRunner>().ExecuteAsync(async token => {
            Recipe? locked = await writes.GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token);
            Assert.NotNull(locked);
            Assert.Equal(IsolationLevel.Serializable, shared.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            Assert.Same(shared.Database.CurrentTransaction!.GetDbTransaction(), owned.Database.CurrentTransaction!.GetDbTransaction());
            locked.UpdateIdentity(name: "Updated owner recipe");
            await writes.UpdateAsync(locked, token);
            await unitOfWork.SaveChangesAsync(token);
            Assert.NotEqual(createdVersion, owned.Entry(locked).Property<uint>("xmin").CurrentValue);
            Recipe? loaded = await writes.GetByIdAsync(recipe.Id, user.Id, includeSteps: true, cancellationToken: token);
            Assert.NotNull(loaded);
            RecipeStep step = Assert.Single(loaded.Steps);
            Assert.Equal("Ingredient", Assert.Single(step.Ingredients, item => item.ProductId.HasValue).ProductSnapshot?.Name);
            Assert.NotNull(Assert.Single(step.Ingredients, item => item.NestedRecipeId.HasValue).NestedRecipe);
            Assert.NotNull(await writes.GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token));
            Assert.Equal(0, await provider.GetRequiredService<IRecipeReadRepository>().GetUsageCountAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token));
            return Result.Success();
        });
        Assert.Equal("Updated owner recipe", (await database.Recipes.AsNoTracking().SingleAsync(item => item.Id == recipe.Id)).Name);
    }

    [RequiresDockerFact]
    public async Task FailureAfterFlushRollsBackBothContextsAndAllowsAnotherAttemptAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        (User user, Recipe recipe) = await SeedAsync(provider);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        RecipesDbContext owned = provider.GetRequiredService<RecipesDbContext>();
        IRecipeRepository writes = provider.GetRequiredService<IRecipeRepository>();
        IRecipeMutationTransactionRunner runner = provider.GetRequiredService<IRecipeMutationTransactionRunner>();
        Result result = await runner.ExecuteAsync(async token => {
            Recipe? locked = await writes.GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token);
            Assert.NotNull(locked);
            locked.UpdateIdentity(name: "Must roll back");
            shared.Users.Add(User.Create("rolled-back-recipe-user@example.com", "hash"));
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            return Result.Failure(new Error("Recipes.TestFailure", "Simulated failure after flush", ErrorKind.Conflict));
        });
        Assert.True(result.IsFailure);
        Assert.Empty(shared.ChangeTracker.Entries());
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Single(await database.Users.ToListAsync());
        Assert.Equal("Owner recipe", (await database.Recipes.AsNoTracking().SingleAsync(item => item.Id == recipe.Id)).Name);
        await runner.ExecuteAsync(async token => {
            Recipe? locked = await writes.GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token);
            Assert.NotNull(locked);
            locked.UpdateIdentity(name: "Next attempt");
            return Result.Success();
        });
        Assert.Equal("Next attempt", (await database.Recipes.AsNoTracking().SingleAsync(item => item.Id == recipe.Id)).Name);
    }

    [RequiresDockerFact]
    public async Task OwnerRowLockBlocksIndependentMutationAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        string connectionString = database.Database.GetConnectionString()!;
        await using ServiceProvider first = CreateProvider(connectionString);
        await using ServiceProvider second = CreateProvider(connectionString, enableRetries: false);
        (User user, Recipe recipe) = await SeedAsync(first);
        await first.GetRequiredService<IRecipeMutationTransactionRunner>().ExecuteAsync(async token => {
            Assert.NotNull(await first.GetRequiredService<IRecipeRepository>().GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: token));
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                second.GetRequiredService<IRecipeMutationTransactionRunner>().ExecuteAsync(async secondToken => {
                    await second.GetRequiredService<FoodDiaryDbContext>().Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'", secondToken);
                    return await second.GetRequiredService<IRecipeRepository>().GetByIdForUpdateAsync(recipe.Id, user.Id, includePublic: false, cancellationToken: secondToken);
                }));
            PostgresException exception = Assert.IsType<PostgresException>(failure.InnerException);
            Assert.Equal(PostgresErrorCodes.LockNotAvailable, exception.SqlState);
            return Result.Success();
        });
        Assert.Empty(second.GetRequiredService<RecipesDbContext>().ChangeTracker.Entries());
    }

    private static Recipe CreateRecipe(User user) => Recipe.Create(user.Id, "Owner recipe", servings: 1);

    private static async Task<(User User, Recipe Recipe)> SeedAsync(ServiceProvider provider) {
        var user = User.Create("recipe-owner@example.com", "hash");
        Recipe recipe = CreateRecipe(user);
        provider.GetRequiredService<FoodDiaryDbContext>().Users.Add(user);
        await provider.GetRequiredService<IRecipeRepository>().AddAsync(recipe);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return (user, recipe);
    }

    private static ServiceProvider CreateProvider(string connectionString, bool enableRetries = true) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = enableRetries.ToString(),
            ["Database:MaxRetryDelaySeconds"] = "1",
        }).Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddProductsPersistence();
        services.AddRecipesPersistence();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
