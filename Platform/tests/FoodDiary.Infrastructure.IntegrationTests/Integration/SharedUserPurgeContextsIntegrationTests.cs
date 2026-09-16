using FoodDiary.Modules.Users.Infrastructure;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.RecentItems.Infrastructure;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecentItems.Domain.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Infrastructure;
using FoodDiary.Modules.MealPlanning.Infrastructure;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Identity.Infrastructure;
using FoodDiary.Modules.Hydration.Infrastructure;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Persistence.Runtime.Persistence;

using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Admin.Domain.Entities;
using FoodDiary.Modules.Admin.Infrastructure;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Ai.Infrastructure;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.BodyMetrics.Infrastructure;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.Cycles.Infrastructure;
using FoodDiary.Modules.Dietologist.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedUserPurgeContextsIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OwnerPurge_RollsBackAndReusesScopeWithCurrentTransactionAsync(bool resolveBeforeTransaction) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser target = Seed(central, "target");
        SeededUser survivor = Seed(central, "survivor");
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[]? participants = resolveBeforeTransaction ? Participants(provider) : null;

        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            participants ??= Participants(provider);
            await PurgeAsync(participants, target.User.Id);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await AssertDataAsync(central, target, exists: false);
            await AssertDataAsync(central, survivor, exists: true);
            await transaction.RollbackAsync();
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, target, exists: true);
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await PurgeAsync(participants, target.User.Id);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await AssertDataAsync(verification, target, exists: false);
        await AssertDataAsync(verification, survivor, exists: true);
        Assert.True(await verification.Users.AnyAsync(user => user.Id == target.User.Id));
        // Entry cleanup preserves replay receipts until the Users owner deletes the user row.
        Assert.True(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == target.User.Id));
        Assert.True(await verification.Set<MealRecognitionReceipt>().AnyAsync(item => item.UserId == target.User.Id));
    }

    [RequiresDockerFact]
    public async Task Cleanup_ContinuesWithNextUserAfterOwnerDeletionRollsBackAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser failed = Seed(central, "failed");
        SeededUser removed = Seed(central, "removed");
        SeededUser survivor = Seed(central, "survivor");
        failed.User.MarkDeleted(DateTime.UtcNow.AddDays(-20));
        removed.User.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[] participants = [.. Participants(provider), new FailingParticipant(failed.User.Id)];
        var service = new UserCleanupService(provider.GetRequiredService<UsersDbContext>(), participants, NullLogger<UserCleanupService>.Instance,
            provider.GetRequiredService<FoodDiary.Persistence.Abstractions.IModuleTransactionCoordinator>(),
            provider.GetRequiredService<FoodDiary.Persistence.Abstractions.IModuleScopeGuard>());

        Assert.Equal(1, (await service.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null)).RemovedCount);

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, failed, exists: true);
        await AssertDataAsync(verification, removed, exists: false);
        await AssertDataAsync(verification, survivor, exists: true);
        Assert.True(await verification.Users.AnyAsync(user => user.Id == failed.User.Id));
        Assert.False(await verification.Users.AnyAsync(user => user.Id == removed.User.Id));
        Assert.True(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == failed.User.Id));
        Assert.False(await verification.Set<HydrationOperationReceipt>().AnyAsync(item => item.UserId == removed.User.Id));
        Assert.True(await verification.Set<MealRecognitionReceipt>().AnyAsync(item => item.UserId == failed.User.Id));
        Assert.False(await verification.Set<MealRecognitionReceipt>().AnyAsync(item => item.UserId == removed.User.Id));
    }

    [RequiresDockerFact]
    public async Task OwnerPurge_CancellationPreservesDataAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        SeededUser target = Seed(central, "canceled");
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IUserDataPurgeParticipant[] participants = Participants(provider);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        foreach (IUserDataPurgeParticipant participant in participants) {
            await using IDbContextTransaction transaction = await central.Database.BeginTransactionAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                participant.PurgeAsync(target.User.Id, reassignTarget: null, cancellation.Token));
            await transaction.RollbackAsync();
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        await AssertDataAsync(verification, target, exists: true);
    }

    private static SeededUser Seed(FoodDiaryDbContext context, string name) {
        var user = User.Create($"purge-{name}@example.com", "hash");
        var peer = User.Create($"purge-{name}-peer@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var entry = HydrationEntry.Create(user.Id, now, 250);
        var profile = CycleProfile.Create(user.Id, today.AddDays(-28));
        profile.UpsertBleedingEntry(today.AddDays(-3), BleedingType.Bleeding, CycleFlowLevel.Medium, 4, "start");
        profile.UpsertSymptomEntry(today, CycleSymptomCategory.Pain, 3, ["lower"], "minor");
        profile.UpsertFactor(CycleFactorType.NonHormonalContraception, today.AddDays(-5), today.AddDays(-1), "tracking");
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, now);
        profile.UpsertFertilitySignal(today, 36.7, OvulationTestResult.Negative, "sticky", hadSex: false, notes: "signal");
        profile.ConfirmPeriodStart(today.AddDays(-3));
        var recipe = Recipe.Create(peer.Id, "Shared recipe", servings: 1);
        var meal = Meal.Create(user.Id, now);
        meal.AddRecipe(recipe.Id, 1);
        MealAiSession session = meal.AddAiSession(imageAssetId: null, AiRecognitionSource.Photo, now, notes: null,
            [new MealAiItemData("Apple", nameLocal: null, 100, "g", 52, 0.3, 0.2, 14, 2.4, 0)]);
        var list = ShoppingList.Create(user.Id, "Purge list");
        list.AddItem("Apple", productId: null, amount: 100, MeasurementUnit.G, category: null, isChecked: false, sortOrder: 0);
        var image = ImageAsset.Create(user.Id, $"purge/{name}.jpg", "https://example.com/purge.jpg");
        var ownedProduct = Product.Create(user.Id, "Owned apple", MeasurementUnit.G, 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, imageAssetId: image.Id);
        var ownedRecipe = Recipe.Create(user.Id, "Owned recipe", servings: 1, imageAssetId: image.Id);
        ownedRecipe.AddStep(1, "Prepare", imageAssetId: image.Id);
        context.AddRange(user, entry, HydrationOperationReceipt.Create(Guid.NewGuid(), entry),
            WeightEntry.Create(user.Id, now, 72.5), WaistEntry.Create(user.Id, now, 84), profile);
        context.AddRange(peer, recipe, meal, list, image, ownedProduct, ownedRecipe,
            AdminImpersonationSession.Start(user.Id, peer.Id, "Purge actor test", actorIpAddress: null, actorUserAgent: null, now),
            AdminImpersonationSession.Start(peer.Id, user.Id, "Purge target test", actorIpAddress: null, actorUserAgent: null, now),
            ClientTask.Create(user.Id, peer.Id, "Dietologist task", details: null, dueAtUtc: null),
            ClientTask.Create(peer.Id, user.Id, "Client task", details: null, dueAtUtc: null),
            RecentItem.Create(user.Id, RecentItemType.Recipe, recipe.Id.Value, now),
            AiUsage.Create(user.Id, "food", "offline-test", 10, 5, 15),
            new FoodRecognitionJob {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ImageAssetId = image.Id,
                ImageUrl = image.Url,
                CreatedOnUtc = now,
                UpdatedOnUtc = now,
            },
            new TelegramOperation {
                Id = Guid.NewGuid(),
                BotId = 1,
                UpdateId = now.Ticks + user.Id.Value.GetHashCode(),
                UserId = user.Id.Value,
                PayloadHash = "test-hash",
                ProtectedPayload = "test-payload",
                CreatedAtUtc = now,
                NextAttemptAtUtc = now,
                Completed = true,
            },
            MealRecognitionReceipt.Create(Guid.NewGuid(), user.Id, Guid.NewGuid(), meal.Id, 1, now, now, TimeSpan.FromMinutes(5)));
        return new SeededUser(user, profile.Id, peer.Id, recipe.Id, meal.Id, session.Id, list.Id, ownedProduct.Id, ownedRecipe.Id, image.Id, image.ObjectKey);
    }

    private static async Task AssertDataAsync(FoodDiaryDbContext context, SeededUser seeded, bool exists) {
        Assert.Equal(exists, await context.HydrationEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.WeightEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.WaistEntries.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.CycleProfiles.AnyAsync(item => item.Id == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleBleedingEntries.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleSymptomEntries.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleFactors.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.FertilitySignals.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.CycleMenstrualEpisodes.AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists, await context.Set<CycleConsent>().AnyAsync(item => item.CycleProfileId == seeded.ProfileId));
        Assert.Equal(exists ? 2 : 0, await context.AdminImpersonationSessions.CountAsync(item =>
            item.ActorUserId == seeded.User.Id || item.TargetUserId == seeded.User.Id));
        Assert.Equal(exists ? 2 : 0, await context.ClientTasks.CountAsync(item =>
            item.DietologistUserId == seeded.User.Id || item.ClientUserId == seeded.User.Id));
        Assert.Equal(exists, await context.Meals.AnyAsync(item => item.Id == seeded.MealId));
        Assert.Equal(exists, await context.MealItems.AnyAsync(item => item.MealId == seeded.MealId));
        Assert.Equal(exists, await context.MealAiSessions.AnyAsync(item => item.MealId == seeded.MealId));
        Assert.Equal(exists, await context.MealAiItems.AnyAsync(item => item.MealAiSessionId == seeded.SessionId));
        Assert.Equal(exists, await context.ShoppingLists.AnyAsync(item => item.Id == seeded.ListId));
        Assert.Equal(exists, await context.ShoppingListItems.AnyAsync(item => item.ShoppingListId == seeded.ListId));
        Assert.Equal(exists, await context.RecentItems.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.AiUsages.AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.Set<FoodRecognitionJob>().AnyAsync(item => item.UserId == seeded.User.Id));
        Assert.Equal(exists, await context.Set<TelegramOperation>().AnyAsync(item => item.UserId == seeded.User.Id.Value));
        Assert.Equal(exists, await context.Products.AnyAsync(item => item.Id == seeded.ProductId));
        Assert.Equal(exists, await context.Recipes.AnyAsync(item => item.Id == seeded.OwnedRecipeId));
        Assert.Equal(exists, await context.RecipeSteps.AnyAsync(item => item.RecipeId == seeded.OwnedRecipeId));
        Assert.Equal(exists, await context.ImageAssets.AnyAsync(item => item.Id == seeded.ImageId));
        bool[] destinations = await context.Set<ImageObjectDeletionOutboxMessage>()
            .Where(item => item.ObjectKey == seeded.ObjectKey).OrderBy(item => item.IsConfirmed)
            .Select(item => item.IsConfirmed).ToArrayAsync();
        bool[] expectedDestinations = exists ? [] : [false, true];
        Assert.Equal(expectedDestinations, destinations);
        Assert.True(await context.Users.AnyAsync(item => item.Id == seeded.PeerId));
        Assert.True(await context.Recipes.AnyAsync(item => item.Id == seeded.RecipeId));
    }

    private static async Task PurgeAsync(IEnumerable<IUserDataPurgeParticipant> participants, UserId userId) {
        foreach (IUserDataPurgeParticipant participant in participants) {
            await participant.PurgeAsync(userId, reassignTarget: null, CancellationToken.None);
        }
    }

    private static IUserDataPurgeParticipant[] Participants(ServiceProvider provider) {
        IUserDataPurgeParticipant[] participants = [.. provider.GetServices<IUserDataPurgeParticipant>().OrderBy(item => item.Order)];
        Assert.Equal([10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 120, 130, 135], participants.Select(item => item.Order));
        return participants;
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext central) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddUsersPersistence();
        services.AddSingleton(central);
        services.AddSingleton<SharedPersistenceDbContext>(central);
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddHydrationModule().AddBodyMetricsModule().AddCyclesModule();
        services.AddAdminPersistence().AddDietologistModule().AddMealsPersistence().AddMealPlanningModule()
            .AddRecentItemsModule().AddAiPersistence().AddIdentityPersistence().AddProductsPersistence().AddRecipesPersistence()
            .AddImagesInfrastructure();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed record SeededUser(User User, CycleProfileId ProfileId, UserId PeerId, RecipeId RecipeId, MealId MealId,
        MealAiSessionId SessionId, ShoppingListId ListId, ProductId ProductId, RecipeId OwnedRecipeId, ImageAssetId ImageId, string ObjectKey);

    [ExcludeFromCodeCoverage]
    private sealed class FailingParticipant(UserId target) : IUserDataPurgeParticipant {
        public int Order => 140;

        public Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) =>
            userId == target ? throw new InvalidOperationException("Injected failure after the owner deletions.") : Task.CompletedTask;
    }
}
