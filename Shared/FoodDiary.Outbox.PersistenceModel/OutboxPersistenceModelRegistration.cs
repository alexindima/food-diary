using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Outbox.PersistenceModel;

public static class OutboxPersistenceModelRegistration {
    public static ModelBuilder ApplyOutboxPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboxPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
