using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.PersistenceModel;

public static class IdentityPersistenceModelRegistration {
    public static ModelBuilder ApplyIdentityPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
