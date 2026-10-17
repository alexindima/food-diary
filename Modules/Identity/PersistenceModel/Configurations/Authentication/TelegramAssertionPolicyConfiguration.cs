using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Identity.PersistenceModel.Configurations.Authentication;

internal sealed class TelegramAssertionPolicyConfiguration : IEntityTypeConfiguration<TelegramAssertionPolicy> {
    public void Configure(EntityTypeBuilder<TelegramAssertionPolicy> builder) {
        builder.ToTable("TelegramAssertionPolicies", table => table.HasCheckConstraint("CK_TelegramAssertionPolicies_Singleton", "\"Id\" = 1"));
        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.Id).ValueGeneratedNever();
        builder.Property(policy => policy.NotBeforeUtc).HasColumnType("timestamp with time zone");
    }
}
