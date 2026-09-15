using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.DailyAdvices.PersistenceModel;

public static class DailyAdvicesPersistenceModelRegistration {
    public static ModelBuilder ApplyDailyAdvicesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DailyAdvicesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
