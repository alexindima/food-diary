using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Marketing.Infrastructure.Persistence;

public static class MarketingPersistenceModelRegistration {
    public static ModelBuilder ApplyMarketingPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketingPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
