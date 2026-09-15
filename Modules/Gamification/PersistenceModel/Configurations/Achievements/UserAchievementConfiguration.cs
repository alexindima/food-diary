using FoodDiary.Modules.Gamification.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Gamification.PersistenceModel.Configurations.Achievements;

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

        builder.HasIndex(achievement => new { achievement.UserId, achievement.AchievementKey })
            .IsUnique();
        builder.HasIndex(achievement => new { achievement.UserId, achievement.EarnedAtUtc });
    }
}
