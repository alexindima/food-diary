using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Admin;


internal sealed class AdminImpersonationSessionConfiguration : IEntityTypeConfiguration<AdminImpersonationSession> {
    public void Configure(EntityTypeBuilder<AdminImpersonationSession> builder) {
        builder.Property(e => e.ActorUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.TargetUserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ActorIpAddress)
            .HasMaxLength(128);

        builder.Property(e => e.ActorUserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.StartedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.ActorUserId);
        builder.HasIndex(e => e.TargetUserId);
        builder.HasIndex(e => e.StartedAtUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
