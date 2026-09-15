using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.PersistenceModel;

public static class RecipesPersistenceModelRegistration {
    public static ModelBuilder ApplyRecipesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
