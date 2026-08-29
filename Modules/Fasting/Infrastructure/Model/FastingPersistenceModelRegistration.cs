using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence;

public static class FastingPersistenceModelRegistration {
    public static ModelBuilder ApplyFastingPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FastingPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
