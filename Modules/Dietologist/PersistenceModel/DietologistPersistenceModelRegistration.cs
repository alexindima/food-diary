using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.PersistenceModel;

public static class DietologistPersistenceModelRegistration {
    public static ModelBuilder ApplyDietologistPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietologistPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
