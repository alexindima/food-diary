using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Recipes.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class RecipeModerationIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupersessionRollsBackAfterFlushForEarlyAndLateContextResolutionAsync(bool resolveBeforeTransaction) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, CreateProviderStub("OpenAi"));
        RecipeModel original = await CreatePendingAsync(provider, author.Id);
        Guid originalId = GetRevisionId(original);
        var recipeId = new RecipeId(original.Id);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        SharedPersistenceDbContext shared = services.GetRequiredService<SharedPersistenceDbContext>();
        IRecipePublicationQueue? earlyQueue = resolveBeforeTransaction ? services.GetRequiredService<IRecipePublicationQueue>() : null;
        const string rollbackCode = "Recipes.TestRollback";

        Result rolledBack = await services.GetRequiredService<IRecipeMutationTransactionRunner>().ExecuteAsync(async token => {
            IRecipePublicationQueue queue = earlyQueue ?? services.GetRequiredService<IRecipePublicationQueue>();
            RecipesDbContext owned = services.GetRequiredService<RecipesDbContext>();
            Assert.NotNull(shared.Database.CurrentTransaction);
            Assert.Same(shared.Database.CurrentTransaction.GetDbTransaction(), owned.Database.CurrentTransaction?.GetDbTransaction());
            RecipePublicationState replacement = await queue.StageAsync(
                recipeId, author.Id, "{}", new RecipeModerationContent("Replacement soup", []), token);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);

            Assert.Same(shared.Database.CurrentTransaction.GetDbTransaction(), owned.Database.CurrentTransaction?.GetDbTransaction());
            Assert.Equal(replacement.RevisionId, (await queue.GetCurrentAsync(recipeId, token))?.RevisionId);
            Assert.True((await ReadRevisionAsync(database, originalId)).IsCurrent);
            return Result.Failure(new Error(rollbackCode, "Forced rollback after replacing the current revision."));
        });

        ResultAssert.Failure(rolledBack, rollbackCode);
        Assert.Null(services.GetRequiredService<RecipesDbContext>().Database.CurrentTransaction);
        Assert.Equal(originalId, (await services.GetRequiredService<IRecipePublicationQueue>().GetCurrentAsync(recipeId, CancellationToken.None))?.RevisionId);
        RecipePublicationRevision persisted = Assert.Single(await database.Set<RecipePublicationRevision>().AsNoTracking()
            .Where(revision => revision.RecipeId == recipeId).ToListAsync());
        Assert.Multiple(
            () => Assert.Equal(originalId, persisted.Id),
            () => Assert.True(persisted.IsCurrent),
            () => Assert.Equal(RecipePublicationStatuses.Pending, persisted.Status));
    }

    [RequiresDockerFact]
    public async Task BatchLookupUsesCurrentRevisionAndPublicDisplayStatusAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);
        RecipeModel original = await CreatePendingAsync(provider, author.Id);
        RecipeModel replacement = await ReplacePendingAsync(provider, author.Id, original.Id, "Bean soup");
        var recipeId = new RecipeId(original.Id);
        var missingId = new RecipeId(Guid.NewGuid());
        Guid replacementId = GetRevisionId(replacement);
        Assert.NotEqual(GetRevisionId(original), replacementId);
        Assert.True(await ProcessAsync(provider));
        Assert.Equal(RecipePublicationStatuses.Approved, (await ReadRevisionAsync(database, replacementId)).Status);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipePublicationQueue queue = scope.ServiceProvider.GetRequiredService<IRecipePublicationQueue>();

        Assert.Empty(await queue.GetCurrentAsync([], CancellationToken.None));
        IReadOnlyDictionary<RecipeId, RecipePublicationState> states = await queue.GetCurrentAsync(
            [recipeId, missingId, recipeId], CancellationToken.None);

        KeyValuePair<RecipeId, RecipePublicationState> state = Assert.Single(states);
        Assert.Multiple(
            () => Assert.Equal(recipeId, state.Key),
            () => Assert.Equal(replacementId, state.Value.RevisionId),
            () => Assert.Equal(RecipePublicationStatuses.Pending, state.Value.Status),
            () => Assert.Null(state.Value.Reason));

        Assert.True(await ProcessAsync(provider));
        Assert.Empty(await queue.GetCurrentAsync([recipeId], CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task CompletionRejectsAnIncorrectTokenAndExpiresAtTheLeaseBoundaryAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        var clock = new ModerationClock(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, clock: clock);
        RecipeModel pending = await CreatePendingAsync(provider, author.Id);
        var recipeId = new RecipeId(pending.Id);
        Guid revisionId = GetRevisionId(pending);
        Assert.True(await ProcessAsync(provider));
        var current = new RecipePublicationClaim(revisionId, Guid.NewGuid());
        var stale = new RecipePublicationClaim(revisionId, Guid.NewGuid());
        DateTimeOffset expiresAt = clock.GetUtcNow().AddMinutes(5);
        await database.Set<RecipePublicationRevision>().Where(revision => revision.Id == revisionId)
            .ExecuteUpdateAsync(update => update.SetProperty(revision => revision.LeaseToken, (Guid?)current.LeaseToken)
                .SetProperty(revision => revision.LeaseUntil, (DateTimeOffset?)expiresAt));
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IRecipePublicationQueue queue = scope.ServiceProvider.GetRequiredService<IRecipePublicationQueue>();

        Assert.True(await queue.CanApplyAsync(recipeId, current, CancellationToken.None));
        Assert.False(await queue.CompleteAsync(recipeId, stale, CancellationToken.None));

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.False(await queue.CanApplyAsync(recipeId, current, CancellationToken.None));
        Assert.False(await queue.CompleteAsync(recipeId, current, CancellationToken.None));
        RecipePublicationRevision unchanged = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Approved, unchanged.Status),
            () => Assert.True(unchanged.IsCurrent),
            () => Assert.Equal(current.LeaseToken, unchanged.LeaseToken),
            () => Assert.Equal(expiresAt, unchanged.LeaseUntil));
    }
}
