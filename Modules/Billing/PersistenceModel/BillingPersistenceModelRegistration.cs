using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.PersistenceModel;

public static class BillingPersistenceModelRegistration {
    public static ModelBuilder ApplyBillingPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
