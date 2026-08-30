using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

public static class BodyMetricsPersistenceModelRegistration {
    public static ModelBuilder ApplyBodyMetricsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BodyMetricsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
