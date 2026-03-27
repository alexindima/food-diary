using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.ProfileImageAssetId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new ImageAssetId(value.Value) : null);

        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.IsEmailConfirmed).HasDefaultValue(false);
        entity.Property(e => e.EmailConfirmationTokenExpiresAtUtc)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.EmailConfirmationSentAtUtc)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.PasswordResetTokenExpiresAtUtc)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.PasswordResetSentAtUtc)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.LastLoginAtUtc)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.DeletedAt)
            .HasColumnType("timestamp with time zone");
        entity.Property(e => e.ActivityLevel)
            .HasConversion<string>()
            .HasDefaultValue(ActivityLevel.Moderate);
        entity.Property(e => e.Language)
            .HasDefaultValue("en");
        entity.Property(e => e.TelegramUserId)
            .HasColumnType("bigint");
        entity.Property(e => e.DashboardLayoutJson)
            .HasColumnType("jsonb")
            .HasColumnName("DashboardLayout");
        entity.Property(e => e.AiInputTokenLimit)
            .HasDefaultValue(5_000_000L);
        entity.Property(e => e.AiOutputTokenLimit)
            .HasDefaultValue(1_000_000L);

        entity.HasMany(e => e.WeightEntries)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.WaistEntries)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.HydrationEntries)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<ImageAsset>()
            .WithMany()
            .HasForeignKey(e => e.ProfileImageAssetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(e => e.TelegramUserId)
            .IsUnique();
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role> {
    public void Configure(EntityTypeBuilder<Role> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new RoleId(value));

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);

        entity.HasIndex(e => e.Name)
            .IsUnique();

        entity.Metadata.FindNavigation(nameof(Role.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole> {
    public void Configure(EntityTypeBuilder<UserRole> entity) {
        entity.HasKey(e => new { e.UserId, e.RoleId });

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.RoleId).HasConversion(
            id => id.Value,
            value => new RoleId(value));

        entity.HasOne(e => e.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
