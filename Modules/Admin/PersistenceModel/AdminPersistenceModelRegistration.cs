using FoodDiary.Modules.Admin.PersistenceModel.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Admin.PersistenceModel;

public static class AdminPersistenceModelRegistration {
    public static ModelBuilder ApplyAdminPersistenceModel(this ModelBuilder modelBuilder) {
        modelBuilder.ApplyConfiguration(new AdminImpersonationSessionConfiguration());
        modelBuilder.Entity<BugAcknowledgementReceipt>(entity => {
            entity.ToTable("BugAcknowledgementReceipts");
            entity.HasKey(x => x.InboxId);
        });
        return modelBuilder;
    }
}
