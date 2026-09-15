using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Gamification.PersistenceModel;

public static class GamificationPersistenceModelRegistration {
    public static ModelBuilder ApplyGamificationPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamificationPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
