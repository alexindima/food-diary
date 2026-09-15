using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Notifications.PersistenceModel;

public static class NotificationsPersistenceModelRegistration {
    public static ModelBuilder ApplyNotificationsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
