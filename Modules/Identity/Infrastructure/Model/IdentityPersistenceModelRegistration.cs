using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class IdentityPersistenceModelRegistration {
    public static ModelBuilder ApplyIdentityPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
