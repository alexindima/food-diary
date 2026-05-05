using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class UserLoginEventConfiguration : IEntityTypeConfiguration<UserLoginEvent> {
    public void Configure(EntityTypeBuilder<UserLoginEvent> entity) {
        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.AuthProvider)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.IpAddress)
            .HasMaxLength(128);

        entity.Property(e => e.UserAgent)
            .HasMaxLength(512);

        entity.Property(e => e.BrowserName)
            .HasMaxLength(64);

        entity.Property(e => e.BrowserVersion)
            .HasMaxLength(64);

        entity.Property(e => e.OperatingSystem)
            .HasMaxLength(64);

        entity.Property(e => e.DeviceType)
            .HasMaxLength(32);

        entity.Property(e => e.LoggedInAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.LoggedInAtUtc);
        entity.HasIndex(e => e.AuthProvider);
        entity.HasIndex(e => e.DeviceType);
        entity.HasIndex(e => e.BrowserName);
        entity.HasIndex(e => e.OperatingSystem);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
