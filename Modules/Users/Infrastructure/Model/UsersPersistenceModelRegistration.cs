using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure;

public static class UsersPersistenceModelRegistration {
    public static ModelBuilder ApplyUsersPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}

