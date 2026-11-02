using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserPrivacyConsentEventConfiguration : IEntityTypeConfiguration<UserPrivacyConsentEvent> {
    public void Configure(EntityTypeBuilder<UserPrivacyConsentEvent> builder) {
        builder.ToTable("UserPrivacyConsentEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.UserId).HasConversion(id => id.Value, id => new UserId(id));
        builder.Property(item => item.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.NoticeVersion).HasMaxLength(64).IsRequired();
        builder.Property(item => item.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.HasOne<User>().WithMany(user => user.PrivacyConsentEvents).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.UserId, item.OccurredAtUtc });
    }
}
