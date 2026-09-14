using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.BodyMetrics.PersistenceModel;

public static class BodyMetricsPersistenceModelRegistration {
    public static ModelBuilder ApplyBodyMetricsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BodyMetricsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
