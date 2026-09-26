using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Audit.PersistenceModel;

public static class AuditPersistenceModelRegistration {
    public static ModelBuilder ApplyAuditPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
