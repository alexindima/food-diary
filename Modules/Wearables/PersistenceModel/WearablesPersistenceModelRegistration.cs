using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Wearables.PersistenceModel;

public static class WearablesPersistenceModelRegistration {
    public static ModelBuilder ApplyWearablesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WearablesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
