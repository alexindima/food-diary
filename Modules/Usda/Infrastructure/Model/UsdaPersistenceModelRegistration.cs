using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Usda.Infrastructure.Model;

public static class UsdaPersistenceModelRegistration {
    public static ModelBuilder ApplyUsdaPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsdaPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
