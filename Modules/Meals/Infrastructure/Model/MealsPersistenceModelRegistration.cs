using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class MealsPersistenceModelRegistration {
    public static ModelBuilder ApplyMealsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
