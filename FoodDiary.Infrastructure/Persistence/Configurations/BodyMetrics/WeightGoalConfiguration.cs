using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.BodyMetrics;

internal sealed class WeightGoalConfiguration : IEntityTypeConfiguration<WeightGoal> {
    public void Configure(EntityTypeBuilder<WeightGoal> builder) {
        builder.Property(goal => goal.Id).HasConversion(id => id.Value, value => new WeightGoalId(value));
        builder.Property(goal => goal.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(goal => goal.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(goal => goal.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(goal => goal.EndedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(goal => goal.EndWeight);
        builder.HasIndex(goal => goal.UserId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
    }
}
