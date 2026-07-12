using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Authentication;

internal sealed class UserLoginEventConfiguration : IEntityTypeConfiguration<UserLoginEvent> {
    public void Configure(EntityTypeBuilder<UserLoginEvent> builder) {
        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.AuthProvider)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(128);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.BrowserName)
            .HasMaxLength(64);

        builder.Property(e => e.BrowserVersion)
            .HasMaxLength(64);

        builder.Property(e => e.OperatingSystem)
            .HasMaxLength(64);

        builder.Property(e => e.DeviceType)
            .HasMaxLength(32);

        builder.Property(e => e.LoggedInAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.LoggedInAtUtc);
        builder.HasIndex(e => e.AuthProvider);
        builder.HasIndex(e => e.DeviceType);
        builder.HasIndex(e => e.BrowserName);
        builder.HasIndex(e => e.OperatingSystem);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
