using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Lessons.Infrastructure.Persistence;

public static class LessonsPersistenceModelRegistration {
    public static ModelBuilder ApplyLessonsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
