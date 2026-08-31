using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Model;

public static class MealPlanningPersistenceModelRegistration {
    public static ModelBuilder ApplyMealPlanningPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlanningPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
