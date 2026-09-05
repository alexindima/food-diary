using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Outbox;

public static class OutboxPersistenceModelRegistration {
    public static ModelBuilder ApplyOutboxPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboxPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
