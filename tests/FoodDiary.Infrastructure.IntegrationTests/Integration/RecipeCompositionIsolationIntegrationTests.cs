using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Persistence.Recipes;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecipeCompositionIsolationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ConcurrentOppositeRecipeEdges_RetryAndRejectTheCycle() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("composition-cycle@example.com", "hash");
        var first = Recipe.Create(user.Id, "First", 1);
        var second = Recipe.Create(user.Id, "Second", 1);
        seed.AddRange(user, first, second);
        await seed.SaveChangesAsync();
        await using FoodDiaryDbContext left = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        await using FoodDiaryDbContext right = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        async Task<Result> AddEdgeAsync(FoodDiaryDbContext context, RecipeId from, RecipeId to, CancellationToken token) {
            bool cycle = await context.RecipeIngredients.AsNoTracking()
                .AnyAsync(item => item.RecipeStep.RecipeId == to && item.NestedRecipeId == from, token);
            Recipe recipe = await context.Recipes.SingleAsync(item => item.Id == from, token);
            if (Interlocked.Increment(ref attempts) == 2) { bothRead.TrySetResult(); }
            await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
            if (cycle) { return Result.Failure(Errors.Validation.Invalid("Recipe", "Cycle")); }
            recipe.AddStep(1, "Mix").AddNestedRecipeIngredient(to, 1);
            return Result.Success();
        }
        Task<Result> firstAttempt = new EfRecipeMutationTransactionRunner(left, new UnitOfWork(left))
            .ExecuteAsync(token => AddEdgeAsync(left, first.Id, second.Id, token));
        Task<Result> secondAttempt = new EfRecipeMutationTransactionRunner(right, new UnitOfWork(right))
            .ExecuteAsync(token => AddEdgeAsync(right, second.Id, first.Id, token));
        Result[] results = await Task.WhenAll(firstAttempt, secondAttempt).WaitAsync(TimeSpan.FromSeconds(45));
        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => result.IsFailure);
        Assert.True(attempts >= 3);
        Assert.Equal(1, await seed.RecipeIngredients.AsNoTracking().CountAsync());
    }

    [RequiresDockerFact]
    public async Task ProductUpdateRacingRecipeReference_RechecksUsageOrReloadsUpdatedProduct() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("composition-product@example.com", "hash");
        var product = Product.Create(user.Id, "Before", MeasurementUnit.G, 100, 100, 52, 1, 1, 11, 2, 0);
        var recipe = Recipe.Create(user.Id, "Recipe", 1);
        seed.AddRange(user, product, recipe);
        await seed.SaveChangesAsync();
        await using FoodDiaryDbContext editing = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        await using FoodDiaryDbContext linking = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        string? linkedName = null;
        async Task SynchronizeAsync(CancellationToken token) {
            if (Interlocked.Increment(ref reads) == 2) { bothRead.TrySetResult(); }
            await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
        }
        Task<Result> edit = new EfProductMutationTransactionRunner(editing, new UnitOfWork(editing)).ExecuteAsync(async token => {
            Product current = await editing.Products.SingleAsync(item => item.Id == product.Id, token);
            bool used = await editing.RecipeIngredients.AsNoTracking().AnyAsync(item => item.ProductId == product.Id, token);
            await SynchronizeAsync(token);
            if (used) { return Result.Failure(Errors.Validation.Invalid("Product", "Used")); }
            current.UpdateCoreIdentity(name: "After");
            return Result.Success();
        });
        Task<Result> link = new EfRecipeMutationTransactionRunner(linking, new UnitOfWork(linking)).ExecuteAsync(async token => {
            linkedName = await linking.Products.AsNoTracking().Where(item => item.Id == product.Id).Select(item => item.Name).SingleAsync(token);
            Recipe current = await linking.Recipes.SingleAsync(item => item.Id == recipe.Id, token);
            await SynchronizeAsync(token);
            current.AddStep(1, "Mix").AddProductIngredient(product.Id, 100);
            return Result.Success();
        });
        await Task.WhenAll(edit, link).WaitAsync(TimeSpan.FromSeconds(45));
        Assert.True((await link).IsSuccess);
        string persistedName = await seed.Products.AsNoTracking().Select(item => item.Name).SingleAsync();
        Assert.Equal(persistedName, linkedName);
        Assert.Equal(1, await seed.RecipeIngredients.AsNoTracking().CountAsync());
        Assert.True(reads >= 3);
    }

    [ExcludeFromCodeCoverage]
    private sealed class UnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
        public bool HasPendingChanges => context.ChangeTracker.HasChanges();
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
    }
}
