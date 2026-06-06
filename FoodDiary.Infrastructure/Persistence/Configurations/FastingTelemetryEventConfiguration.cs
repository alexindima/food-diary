using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class FastingTelemetryEventConfiguration : IEntityTypeConfiguration<FastingTelemetryEvent> {
    public void Configure(EntityTypeBuilder<FastingTelemetryEvent> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new FastingTelemetryEventId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.SessionId)
            .HasMaxLength(64);

        builder.Property(e => e.Protocol)
            .HasMaxLength(32);

        builder.Property(e => e.PlanType)
            .HasMaxLength(16);

        builder.Property(e => e.Status)
            .HasMaxLength(16);

        builder.Property(e => e.OccurrenceKind)
            .HasMaxLength(16);

        builder.Property(e => e.ReminderPresetId)
            .HasMaxLength(32);

        builder.Property(e => e.ReminderSource)
            .HasMaxLength(16);

        builder.Property(e => e.OccurredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => new { e.Name, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.ReminderPresetId, e.Name, e.OccurredAtUtc });
    }
}
