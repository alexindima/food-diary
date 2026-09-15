using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.PersistenceModel;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options) {
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<NotificationWebPushOutboxMessage> NotificationWebPushOutbox => Set<NotificationWebPushOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyNotificationsPersistenceModel();
    }
}
