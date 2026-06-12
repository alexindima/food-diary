using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class UserRefreshTokenSessionConfiguration : IEntityTypeConfiguration<UserRefreshTokenSession> {
    public void Configure(EntityTypeBuilder<UserRefreshTokenSession> builder) {
        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.PreviousRefreshTokenHash)
            .HasMaxLength(512);

        builder.Property(e => e.AuthProvider)
            .HasMaxLength(64);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(128);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastRotatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.PreviousRefreshTokenValidUntilUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.RevokedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.RevokedAtUtc);
        builder.HasIndex(e => e.LastRotatedAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
