using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedDeliveryContextsIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("Images", false)]
    [InlineData("Images", true)]
    [InlineData("Notifications", false)]
    [InlineData("Notifications", true)]
    [InlineData("Gamification", false)]
    [InlineData("Gamification", true)]
    public async Task ProcessorRejectsLatePendingCallerChangesWithoutDiscardingThemAsync(string module, bool foreignModule) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        Func<Task<int>> process = ResolveProcessor(provider, module);
        var user = User.Create($"scope-guard-{Guid.NewGuid():N}@example.com", "hash");
        DbContext dirtyContext;
        if (!foreignModule) {
            central.Users.Add(user);
            dirtyContext = central;
        } else if (string.Equals(module, "Images", StringComparison.Ordinal)) {
            NotificationsDbContext notifications = provider.GetRequiredService<NotificationsDbContext>();
            notifications.Notifications.Add(Notification.Create(user.Id, "test", "{}"));
            dirtyContext = notifications;
        } else {
            ImagesDbContext images = provider.GetRequiredService<ImagesDbContext>();
            images.ImageAssets.Add(ImageAsset.Create(user.Id, "scope-guard", "https://example.com/image"));
            dirtyContext = images;
        }

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(process);
        Assert.Contains("pending changes", error.Message, StringComparison.Ordinal);
        Assert.Equal(EntityState.Added, Assert.Single(dirtyContext.ChangeTracker.Entries()).State);

        dirtyContext.ChangeTracker.Clear();
        Assert.Equal(0, await process());
    }

    [RequiresDockerTheory]
    [InlineData("Images")]
    [InlineData("Notifications")]
    [InlineData("Gamification")]
    public async Task ProcessorRejectsCallerTransactionStartedAfterResolutionAsync(string module) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        Func<Task<int>> process = ResolveProcessor(provider, module);
        await using (IDbContextTransaction transaction = await central.Database.BeginTransactionAsync()) {
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(process);
            Assert.Contains("nested", error.Message, StringComparison.Ordinal);
            Assert.Same(transaction, central.Database.CurrentTransaction);
            await transaction.RollbackAsync();
        }
        Assert.Equal(0, await process());
    }

    private static Func<Task<int>> ResolveProcessor(ServiceProvider provider, string module) {
        switch (module) {
            case "Images":
                IImageObjectDeletionOutboxProcessor images = provider.GetRequiredService<IImageObjectDeletionOutboxProcessor>();
                return () => images.ProcessDueAsync(1);
            case "Notifications":
                INotificationWebPushOutboxProcessor notifications = provider.GetRequiredService<INotificationWebPushOutboxProcessor>();
                return () => notifications.ProcessDueAsync(1);
            case "Gamification":
                IAchievementEvaluationOutboxProcessor achievements = provider.GetRequiredService<IAchievementEvaluationOutboxProcessor>();
                return () => achievements.ProcessDueAsync(1);
            default:
                throw new ArgumentOutOfRangeException(nameof(module));
        }
    }
}
