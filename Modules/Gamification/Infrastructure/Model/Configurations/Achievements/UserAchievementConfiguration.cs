using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Achievements;

internal sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement> {
    public void Configure(EntityTypeBuilder<UserAchievement> builder) {
        builder.ToTable("UserAchievements");

        builder.HasKey(achievement => achievement.Id);
        builder.Property(achievement => achievement.Id)
            .HasConversion(
                id => id.Value,
                value => new UserAchievementId(value))
            .ValueGeneratedNever();

        builder.Property(achievement => achievement.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(achievement => achievement.AchievementKey)
            .IsRequired()
            .HasMaxLength(UserAchievement.AchievementKeyMaxLength);

        builder.Property(achievement => achievement.EarnedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(achievement => achievement.User)
            .WithMany()
            .HasForeignKey(achievement => achievement.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(achievement => new { achievement.UserId, achievement.AchievementKey })
            .IsUnique();
        builder.HasIndex(achievement => new { achievement.UserId, achievement.EarnedAtUtc });
    }
}
