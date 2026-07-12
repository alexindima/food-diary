using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Users;


internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder.Property<uint>("xmin").IsRowVersion();

        ConfigureIdentity(builder);
        ConfigurePreferences(builder);
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

        builder.HasIndex(e => e.Email).IsUnique();
        builder.Property(e => e.IsActive).HasDefaultValue(value: true);
        builder.Property(e => e.IsEmailConfirmed).HasDefaultValue(value: false);
        builder.Property(e => e.HasPassword).HasDefaultValue(value: true);
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
        builder.Property(e => e.ActivityLevel)
            .HasConversion<string>();
        builder.Property(e => e.TelegramUserId)
            .HasColumnType("bigint");
        builder.HasIndex(e => e.TelegramUserId)
            .IsUnique();
    }

    private static void ConfigurePreferences(EntityTypeBuilder<User> builder) {
        builder.Property(e => e.Language)
            .HasDefaultValue("en");
        builder.Property(e => e.Theme)
            .HasDefaultValue("ocean");
        builder.Property(e => e.UiStyle)
            .HasDefaultValue("classic");
        builder.Property(e => e.PushNotificationsEnabled)
            .HasDefaultValue(value: false);
        builder.Property(e => e.FastingPushNotificationsEnabled)
            .HasDefaultValue(value: true);
        builder.Property(e => e.SocialPushNotificationsEnabled)
            .HasDefaultValue(value: true);
        builder.Property(e => e.FastingCheckInReminderHours)
            .HasDefaultValue(12);
        builder.Property(e => e.FastingCheckInFollowUpReminderHours)
            .HasDefaultValue(20);
        builder.Property(e => e.DashboardLayoutJson)
            .HasColumnType("jsonb")
            .HasColumnName("DashboardLayout");
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
        builder.HasMany(e => e.WeightEntries)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.WaistEntries)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.HydrationEntries)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ProfileImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureNavigationAccess(EntityTypeBuilder<User> builder) {
        builder.Metadata.FindNavigation(nameof(User.Meals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.Products))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.Recipes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.WeightEntries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.WaistEntries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.Cycles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.HydrationEntries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.ShoppingLists))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
