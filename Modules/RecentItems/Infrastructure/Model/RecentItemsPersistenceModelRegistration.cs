using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class RecentItemsPersistenceModelRegistration {
    public static ModelBuilder ApplyRecentItemsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecentItemsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
