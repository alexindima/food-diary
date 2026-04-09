using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingOccurrenceConfiguration : IEntityTypeConfiguration<FastingOccurrence> {
    public void Configure(EntityTypeBuilder<FastingOccurrence> entity) {
        entity.ToTable("FastingOccurrences");

        entity.Property(occurrence => occurrence.Id)
            .HasConversion(StronglyTypedIdConverters.FastingOccurrenceIdConverter.Instance);

        entity.Property(occurrence => occurrence.PlanId)
            .HasConversion(StronglyTypedIdConverters.FastingPlanIdConverter.Instance);

        entity.Property(occurrence => occurrence.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        entity.Property(occurrence => occurrence.Kind)
            .HasConversion<string>()
            .HasMaxLength(24);

        entity.Property(occurrence => occurrence.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        entity.Property(occurrence => occurrence.Notes)
            .HasMaxLength(500);

        entity.HasIndex(occurrence => occurrence.PlanId);
        entity.HasIndex(occurrence => occurrence.UserId);
        entity.HasIndex(occurrence => new { occurrence.UserId, occurrence.Status });
        entity.HasIndex(occurrence => new { occurrence.PlanId, occurrence.SequenceNumber })
            .IsUnique();

        entity.HasOne(occurrence => occurrence.Plan)
            .WithMany()
            .HasForeignKey(occurrence => occurrence.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
