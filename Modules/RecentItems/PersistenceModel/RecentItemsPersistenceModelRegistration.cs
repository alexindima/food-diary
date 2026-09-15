using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecentItems.PersistenceModel;

public static class RecentItemsPersistenceModelRegistration {
    public static ModelBuilder ApplyRecentItemsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecentItemsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
