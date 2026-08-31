using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class ProductsPersistenceModelRegistration {
    public static ModelBuilder ApplyProductsPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
