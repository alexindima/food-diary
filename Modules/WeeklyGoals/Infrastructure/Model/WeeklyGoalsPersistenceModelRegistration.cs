using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

public static class WeeklyGoalsPersistenceModelRegistration {
    public static ModelBuilder ApplyWeeklyGoalsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeeklyGoalsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
