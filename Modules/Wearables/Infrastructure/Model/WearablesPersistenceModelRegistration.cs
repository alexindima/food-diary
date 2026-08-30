using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class WearablesPersistenceModelRegistration {
    public static ModelBuilder ApplyWearablesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WearablesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
