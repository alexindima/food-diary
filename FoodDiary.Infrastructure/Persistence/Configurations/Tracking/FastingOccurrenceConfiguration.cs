using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingOccurrenceConfiguration : IEntityTypeConfiguration<FastingOccurrence> {
    public void Configure(EntityTypeBuilder<FastingOccurrence> builder) {
        builder.ToTable("FastingOccurrences");

        builder.Property(occurrence => occurrence.Id)
            .HasConversion(StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance);

        builder.Property(occurrence => occurrence.PlanId)
            .HasConversion(StronglyTypedIdConverters.FastingPlanIdConverter.Instance);

        builder.Property(occurrence => occurrence.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        builder.Property(occurrence => occurrence.Kind)
            .HasConversion<string>()
            .HasMaxLength(24);

        builder.Property(occurrence => occurrence.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(occurrence => occurrence.Notes)
            .HasMaxLength(500);

        builder.Property(occurrence => occurrence.Symptoms)
            .HasMaxLength(200);

        builder.Property(occurrence => occurrence.CheckInNotes)
            .HasMaxLength(500);

        builder.HasIndex(occurrence => occurrence.PlanId);
        builder.HasIndex(occurrence => occurrence.UserId);
        builder.HasIndex(occurrence => new { occurrence.UserId, occurrence.Status });
        builder.HasIndex(occurrence => new { occurrence.PlanId, occurrence.SequenceNumber })
            .IsUnique();

        builder.HasOne(occurrence => occurrence.Plan)
            .WithMany()
            .HasForeignKey(occurrence => occurrence.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
