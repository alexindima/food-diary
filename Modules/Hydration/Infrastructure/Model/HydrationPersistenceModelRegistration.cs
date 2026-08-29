using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public static class HydrationPersistenceModelRegistration {
    public static ModelBuilder ApplyHydrationPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HydrationPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
