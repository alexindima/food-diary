using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class AdminImpersonationSessionConfiguration : IEntityTypeConfiguration<AdminImpersonationSession> {
    public void Configure(EntityTypeBuilder<AdminImpersonationSession> entity) {
        entity.Property(e => e.ActorUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.TargetUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500);

        entity.Property(e => e.ActorIpAddress)
            .HasMaxLength(128);

        entity.Property(e => e.ActorUserAgent)
            .HasMaxLength(512);

        entity.Property(e => e.StartedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => e.ActorUserId);
        entity.HasIndex(e => e.TargetUserId);
        entity.HasIndex(e => e.StartedAtUtc);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
