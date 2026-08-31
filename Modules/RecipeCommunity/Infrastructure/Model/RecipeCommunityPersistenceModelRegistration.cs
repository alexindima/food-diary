using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class RecipeCommunityPersistenceModelRegistration {
    public static ModelBuilder ApplyRecipeCommunityPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipeCommunityPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
