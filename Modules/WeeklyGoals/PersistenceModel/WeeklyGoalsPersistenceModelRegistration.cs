using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.WeeklyGoals.PersistenceModel;

public static class WeeklyGoalsPersistenceModelRegistration {
    public static ModelBuilder ApplyWeeklyGoalsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeeklyGoalsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
