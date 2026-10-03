using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Users.PersistenceModel.Persistence.Configurations.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        ConfigureIdentity(builder);
        ConfigureIndependentState(builder);
        ConfigureUsageLimits(builder);
        ConfigureRelationships(builder);
        ConfigureNavigationAccess(builder);
    }

    private static void ConfigureIdentity(EntityTypeBuilder<User> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.ProfileImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        builder.Property(e => e.Email).IsRequired(false);
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => new { e.GoogleIssuer, e.GoogleSubject })
            .IsUnique()
            .HasFilter("\"GoogleIssuer\" IS NOT NULL AND \"GoogleSubject\" IS NOT NULL");
        builder.Property(e => e.GoogleIssuer).HasMaxLength(200);
        builder.Property(e => e.GoogleSubject).HasMaxLength(255);
        builder.Property(e => e.IsActive).HasDefaultValue(value: true);
        builder.Property(e => e.IsEmailConfirmed).HasDefaultValue(value: false);
        builder.Property(e => e.HasPassword).HasDefaultValue(value: true);
        builder.Property(e => e.MustChangePassword).HasDefaultValue(value: false);
        builder.Property(e => e.SecurityVersion).HasDefaultValue(0L);
        builder.Property(e => e.EmailConfirmationTokenExpiresAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.EmailConfirmationSentAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.PasswordResetTokenExpiresAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.PasswordResetSentAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.LastLoginAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.DeletedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.TelegramUserId)
            .HasColumnType("bigint");
        builder.HasIndex(e => e.TelegramUserId)
            .IsUnique();
        builder.Property(e => e.TelegramOidcIssuer).HasMaxLength(200);
        builder.Property(e => e.TelegramOidcSubject).HasMaxLength(255);
        builder.HasIndex(e => new { e.TelegramOidcIssuer, e.TelegramOidcSubject })
            .IsUnique()
            .HasFilter("\"TelegramOidcIssuer\" IS NOT NULL AND \"TelegramOidcSubject\" IS NOT NULL");
    }

    private static void ConfigureIndependentState(EntityTypeBuilder<User> builder) {
        builder.Ignore(user => user.DashboardLayoutJson);
        builder.Ignore(user => user.Language);
        builder.Ignore(user => user.Theme);
        builder.Ignore(user => user.UiStyle);
        builder.Ignore(user => user.SurfaceStyle);
        builder.Ignore(user => user.PushNotificationsEnabled);
        builder.Ignore(user => user.FastingPushNotificationsEnabled);
        builder.Ignore(user => user.SocialPushNotificationsEnabled);
        builder.Ignore(user => user.FastingCheckInReminderHours);
        builder.Ignore(user => user.FastingCheckInFollowUpReminderHours);
        builder.Ignore(user => user.TimeZoneId);
        builder.Ignore(user => user.BirthDate);
        builder.Ignore(user => user.Gender);
        builder.Ignore(user => user.WeightKg);
        builder.Ignore(user => user.DesiredWeightKg);
        builder.Ignore(user => user.DesiredWaistCm);
        builder.Ignore(user => user.HeightCm);
        builder.Ignore(user => user.ActivityLevel);
        builder.Ignore(user => user.DailyCalorieTarget);
        builder.Ignore(user => user.ProteinTarget);
        builder.Ignore(user => user.FatTarget);
        builder.Ignore(user => user.CarbTarget);
        builder.Ignore(user => user.FiberTarget);
        builder.Ignore(user => user.StepGoal);
        builder.Ignore(user => user.WaterGoal);
        builder.Ignore(user => user.HydrationGoal);
        builder.Ignore(user => user.CalorieCyclingEnabled);
        builder.Ignore(user => user.MondayCalories);
        builder.Ignore(user => user.TuesdayCalories);
        builder.Ignore(user => user.WednesdayCalories);
        builder.Ignore(user => user.ThursdayCalories);
        builder.Ignore(user => user.FridayCalories);
        builder.Ignore(user => user.SaturdayCalories);
        builder.Ignore(user => user.SundayCalories);
        builder.HasOne(user => user.Preferences).WithOne().HasForeignKey<UserPreferences>(state => state.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(user => user.NutritionProfile).WithOne().HasForeignKey<UserNutritionProfile>(state => state.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(user => user.Preferences).AutoInclude();
        builder.Navigation(user => user.NutritionProfile).AutoInclude();
    }

    private static void ConfigureUsageLimits(EntityTypeBuilder<User> builder) {
        builder.Property(e => e.AiInputTokenLimit)
            .HasDefaultValue(5_000_000L);
        builder.Property(e => e.AiOutputTokenLimit)
            .HasDefaultValue(1_000_000L);
        builder.Property(e => e.AiConsentAcceptedAt);
        builder.Property(e => e.PremiumTrialStartedAtUtc)
            .HasColumnType("timestamp with time zone");
        builder.Property(e => e.PremiumTrialEndsAtUtc)
            .HasColumnType("timestamp with time zone");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<User> builder) {
        builder.HasMany(e => e.WeightGoals)
            .WithOne()
            .HasForeignKey(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.WaistGoals)
            .WithOne()
            .HasForeignKey(goal => goal.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureNavigationAccess(EntityTypeBuilder<User> builder) {
        builder.Metadata.FindNavigation(nameof(User.WeightGoals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.WaistGoals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
