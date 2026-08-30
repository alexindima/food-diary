using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;

public static class DailyAdvicesPersistenceModelRegistration {
    public static ModelBuilder ApplyDailyAdvicesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DailyAdvicesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
