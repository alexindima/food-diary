using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Email;

public static class EmailPersistenceModelRegistration {
    public static ModelBuilder ApplyEmailPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
