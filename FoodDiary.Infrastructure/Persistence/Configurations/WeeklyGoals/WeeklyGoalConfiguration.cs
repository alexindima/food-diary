using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.WeeklyGoals;

internal sealed class WeeklyGoalConfiguration : IEntityTypeConfiguration<WeeklyGoal> {
    public void Configure(EntityTypeBuilder<WeeklyGoal> builder) {
        builder.ToTable("WeeklyGoals");
        builder.Property(goal => goal.Id).HasConversion(id => id.Value, value => new WeeklyGoalId(value));
        builder.Property(goal => goal.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(goal => goal.WeekStartUtc).HasColumnType("timestamp with time zone");
        builder.Property(goal => goal.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(goal => goal.TargetDays);
        builder.Property(goal => goal.ReminderTimeMinutes);
        builder.Property(goal => goal.TimeZoneOffsetMinutes);
        builder.Property(goal => goal.LastReminderLocalDate).HasColumnType("date");
        builder.HasIndex(goal => new { goal.UserId, goal.WeekStartUtc }).IsUnique();
        builder.HasIndex(goal => new { goal.ReminderEnabled, goal.WeekStartUtc });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
