using FoodDiary.Infrastructure.Persistence.Configurations.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public static class AdminPersistenceModelRegistration {
    public static ModelBuilder ApplyAdminPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new AdminImpersonationSessionConfiguration());
        return modelBuilder;
    }
}
