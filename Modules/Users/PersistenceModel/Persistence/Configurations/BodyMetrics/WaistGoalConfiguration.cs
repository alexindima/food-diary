using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.BodyMetrics;

internal sealed class WaistGoalConfiguration : IEntityTypeConfiguration<WaistGoal> {
    public void Configure(EntityTypeBuilder<WaistGoal> builder) {
        builder.Property(goal => goal.Id).HasConversion(id => id.Value, value => new WaistGoalId(value));
        builder.Property(goal => goal.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(goal => goal.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(goal => goal.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(goal => goal.EndedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(goal => goal.EndWaistCm);
        builder.HasIndex(goal => goal.UserId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
    }
}
