using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.Infrastructure;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Modules.Images.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Infrastructure;
using FoodDiary.Modules.Notifications.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedDeliveryContextsIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSaveAndProcessorsUseOwnedModelsAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        NotificationsDbContext notifications = provider.GetRequiredService<NotificationsDbContext>();
        ImagesDbContext images = provider.GetRequiredService<ImagesDbContext>();
        GamificationDbContext achievements = provider.GetRequiredService<GamificationDbContext>();
        Assert.Equal(3, notifications.Model.GetEntityTypes().Count());
        Assert.Equal(2, images.Model.GetEntityTypes().Count());
        Assert.Equal(3, achievements.Model.GetEntityTypes().Count());
        Assert.Same(central.Database.GetDbConnection(), notifications.Database.GetDbConnection());
        Assert.Same(central.Database.GetDbConnection(), images.Database.GetDbConnection());
        Assert.Same(central.Database.GetDbConnection(), achievements.Database.GetDbConnection());
        var user = User.Create($"delivery-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        var notification = Notification.Create(user.Id, "test", "{}");
        await provider.GetRequiredService<INotificationWriteRepository>().AddAsync(notification);
        await provider.GetRequiredService<INotificationWebPushOutbox>().EnqueueAsync(notification.Id);
        await provider.GetRequiredService<IImageAssetWriteRepository>().AddAsync(ImageAsset.Create(user.Id, "owned-image", "https://example.com/image"));
        await provider.GetRequiredService<IImageObjectDeletionOutbox>().EnqueueAsync("old-image", isConfirmed: true);
        Assert.Empty(central.ChangeTracker.Entries<Notification>());
        Assert.Empty(central.ChangeTracker.Entries<ImageAsset>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<IImageObjectDeletionOutboxProcessor>().ProcessDueAsync(1));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await provider.GetRequiredService<IAchievementEvaluationOutbox>().EnqueueAsync(user.Id);
        Assert.Equal(1, await provider.GetRequiredService<INotificationWebPushOutboxProcessor>().ProcessDueAsync(1));
        Assert.Equal(1, await provider.GetRequiredService<IImageObjectDeletionOutboxProcessor>().ProcessDueAsync(1));
        Assert.Equal(1, await provider.GetRequiredService<IAchievementEvaluationOutboxProcessor>().ProcessDueAsync(1));
        Assert.NotNull((await central.NotificationWebPushOutbox.AsNoTracking().SingleAsync()).ProcessedOnUtc);
        Assert.NotNull((await central.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync()).ProcessedOnUtc);
        Assert.NotNull((await central.AchievementEvaluationOutbox.AsNoTracking().SingleAsync()).ProcessedOnUtc);
    }

    [RequiresDockerFact]
    public async Task ReferencedImageRollsBackDeletionAndOutboxAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"image-fence-{Guid.NewGuid():N}@example.com", "hash");
        var image = ImageAsset.Create(user.Id, "referenced", "https://example.com/image");
        central.Users.Add(user);
        central.ImageAssets.Add(image);
        central.Products.Add(Product.Create(user.Id, "Reference", MeasurementUnit.G, 100, 100, 10, 1, 1, 1, 1, 0, imageAssetId: image.Id));
        await central.SaveChangesAsync();
        central.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(central);
        IImageAssetWriteRepository repository = provider.GetRequiredService<IImageAssetWriteRepository>();
        Assert.True(await repository.IsAssetInUseAsync(image.Id));
        Assert.Empty(await repository.GetUnusedOlderThanAsync(DateTime.UtcNow.AddDays(1), 10));
        await repository.DeleteAsync(image);
        await provider.GetRequiredService<IImageObjectDeletionOutbox>().EnqueueAsync(image.ObjectKey);
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.True(await read.ImageAssets.AnyAsync(item => item.Id == image.Id));
        Assert.Empty(await read.ImageObjectDeletionOutbox.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task AchievementEnqueueFollowsLateTransactionAndCoalescesAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"achievement-tx-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IAchievementEvaluationOutbox outbox = provider.GetRequiredService<IAchievementEvaluationOutbox>();
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            await outbox.EnqueueAsync(user.Id);
            await transaction.RollbackAsync();
        }
        Assert.Empty(await central.AchievementEvaluationOutbox.AsNoTracking().ToListAsync());
        await outbox.EnqueueAsync(user.Id);
        await outbox.EnqueueAsync(user.Id);
        Assert.Equal(2, (await central.AchievementEvaluationOutbox.AsNoTracking().SingleAsync()).Revision);
    }

    [RequiresDockerFact]
    public async Task UserPurgeCommitsOwnerDeletionOutboxAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"delivery-purge-{Guid.NewGuid():N}@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow.AddDays(-10));
        central.Users.Add(user);
        central.ImageAssets.Add(ImageAsset.Create(user.Id, "purged-image", "https://example.com/image"));
        await central.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var cleanup = new UserCleanupService(central, provider.GetServices<IUserDataPurgeParticipant>(),
            NullLogger<UserCleanupService>.Instance, provider.GetRequiredService<IUnitOfWork>());
        Assert.Equal(1, await cleanup.CleanupDeletedUsersAsync(DateTime.UtcNow.AddDays(-1), 10, reassignUserId: null));
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.ImageAssets.AnyAsync(item => item.UserId == user.Id));
        Assert.Equal(2, await read.ImageObjectDeletionOutbox.CountAsync());
    }

    [RequiresDockerFact]
    public async Task AchievementProcessorRequeuesConcurrentOwnerRevisionAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"delivery-revision-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await central.SaveChangesAsync();
        IAchievementReconciliationHandler handler = Substitute.For<IAchievementReconciliationHandler>();
        handler.ReconcileAsync(user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(async _ => {
            await using FoodDiaryDbContext concurrent = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
            await using ServiceProvider producer = CreateProvider(concurrent);
            await producer.GetRequiredService<IAchievementEvaluationOutbox>().EnqueueAsync(user.Id);
        });
        await using ServiceProvider provider = CreateProvider(central, handler);
        await provider.GetRequiredService<IAchievementEvaluationOutbox>().EnqueueAsync(user.Id);
        Assert.Equal(0, await provider.GetRequiredService<IAchievementEvaluationOutboxProcessor>().ProcessDueAsync(1));
        FoodDiary.Infrastructure.Persistence.Achievements.AchievementEvaluationOutboxMessage message =
            await central.AchievementEvaluationOutbox.AsNoTracking().SingleAsync();
        Assert.Equal(2, message.Revision);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Null(message.LockedBy);
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context, IAchievementReconciliationHandler? reconciliation = null) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddNotificationsPersistence();
        services.AddImagesInfrastructure();
        services.AddGamificationModule();
        services.AddReadModelComposition();
        services.AddSingleton(Substitute.For<IWebPushNotificationSender>());
        services.AddSingleton(Substitute.For<IImageStorageService>());
        services.AddSingleton(reconciliation ?? Substitute.For<IAchievementReconciliationHandler>());
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
