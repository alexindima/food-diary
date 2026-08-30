using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence;

public static class FavoritesPersistenceModelRegistration {
    public static ModelBuilder ApplyFavoritesPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FavoritesPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
