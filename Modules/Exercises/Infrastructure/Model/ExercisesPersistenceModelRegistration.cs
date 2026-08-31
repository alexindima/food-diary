using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class ExercisesPersistenceModelRegistration {
    public static ModelBuilder ApplyExercisesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExercisesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
