using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Cycles.Infrastructure.Persistence;

public static class CyclesPersistenceModelRegistration {
    public static ModelBuilder ApplyCyclesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CyclesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
