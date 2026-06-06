using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Tracking;

internal sealed class FastingPlanConfiguration : IEntityTypeConfiguration<FastingPlan> {
    public void Configure(EntityTypeBuilder<FastingPlan> builder) {
        builder.ToTable("FastingPlans");

        builder.Property(plan => plan.Id)
            .HasConversion(StronglyTypedIdConverters.FastingPlanIdConverter.Instance);

        builder.Property(plan => plan.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        builder.Property(plan => plan.Title)
            .HasMaxLength(120);

        builder.Property(plan => plan.Type)
            .HasConversion<string>()
            .HasMaxLength(24);

        builder.Property(plan => plan.Status)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(plan => plan.Protocol)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(plan => plan.UserId);
        builder.HasIndex(plan => new { plan.UserId, plan.Status });
    }
}
