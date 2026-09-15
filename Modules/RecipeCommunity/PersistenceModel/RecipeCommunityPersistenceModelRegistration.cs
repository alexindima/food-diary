using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecipeCommunity.PersistenceModel;

public static class RecipeCommunityPersistenceModelRegistration {
    public static ModelBuilder ApplyRecipeCommunityPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipeCommunityPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
