using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Outbox;

internal sealed class OutboxReplayAuditConfiguration : IEntityTypeConfiguration<OutboxReplayAudit> {
    public void Configure(EntityTypeBuilder<OutboxReplayAudit> builder) {
        builder.ToTable("OutboxReplayAudits");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.OutboxName).HasMaxLength(64).IsRequired();
        builder.Property(entry => entry.RequestedBy).HasMaxLength(256).IsRequired();
        builder.Property(entry => entry.Reason).HasMaxLength(1024).IsRequired();
        builder.Property(entry => entry.PreviousError).HasMaxLength(2048);
        builder.HasIndex(entry => new { entry.OutboxName, entry.MessageId, entry.RequestedOnUtc });
    }
}
