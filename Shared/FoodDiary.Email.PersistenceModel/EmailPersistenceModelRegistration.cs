using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Email.PersistenceModel;

public static class EmailPersistenceModelRegistration {
    public static ModelBuilder ApplyEmailPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailPersistenceModelRegistration).Assembly);
        return modelBuilder;
    }
}
