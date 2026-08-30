using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Notifications.Infrastructure.Model;

public static class NotificationsPersistenceModelRegistration {
    public static ModelBuilder ApplyNotificationsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
