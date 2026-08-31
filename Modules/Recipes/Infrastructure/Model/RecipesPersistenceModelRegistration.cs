using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class RecipesPersistenceModelRegistration {
    public static ModelBuilder ApplyRecipesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
