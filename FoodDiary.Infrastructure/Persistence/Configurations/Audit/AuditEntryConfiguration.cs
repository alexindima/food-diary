using FoodDiary.Infrastructure.Persistence.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Audit;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry> {
    public void Configure(EntityTypeBuilder<AuditEntry> builder) {
        builder.ToTable("AuditEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Action).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.TargetType).HasMaxLength(80).IsRequired();
        builder.Property(entry => entry.TargetId).HasMaxLength(100);
        builder.Property(entry => entry.Metadata).HasMaxLength(1000);
        builder.HasIndex(entry => entry.CreatedAtUtc);
        builder.HasIndex(entry => new { entry.SubjectClientUserId, entry.CreatedAtUtc });
    }
}
