using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class UserRefreshTokenSessionConfiguration : IEntityTypeConfiguration<UserRefreshTokenSession> {
    public void Configure(EntityTypeBuilder<UserRefreshTokenSession> entity) {
        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.AuthProvider)
            .HasMaxLength(64);

        entity.Property(e => e.IpAddress)
            .HasMaxLength(128);

        entity.Property(e => e.UserAgent)
            .HasMaxLength(512);

        entity.Property(e => e.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.LastRotatedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.RevokedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.RevokedAtUtc);
        entity.HasIndex(e => e.LastRotatedAtUtc);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
