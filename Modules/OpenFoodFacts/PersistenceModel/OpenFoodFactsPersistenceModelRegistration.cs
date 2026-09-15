using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.OpenFoodFacts.PersistenceModel;

public static class OpenFoodFactsPersistenceModelRegistration {
    public static ModelBuilder ApplyOpenFoodFactsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpenFoodFactsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
