using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingPlanConfiguration : IEntityTypeConfiguration<FastingPlan> {
    public void Configure(EntityTypeBuilder<FastingPlan> entity) {
        entity.ToTable("FastingPlans");

        entity.Property(plan => plan.Id)
            .HasConversion(StronglyTypedIdConverters.FastingPlanIdConverter.Instance);

        entity.Property(plan => plan.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        entity.Property(plan => plan.Title)
            .HasMaxLength(120);

        entity.Property(plan => plan.Type)
            .HasConversion<string>()
            .HasMaxLength(24);

        entity.Property(plan => plan.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        entity.Property(plan => plan.Protocol)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.HasIndex(plan => plan.UserId);
        entity.HasIndex(plan => new { plan.UserId, plan.Status });
    }
}
