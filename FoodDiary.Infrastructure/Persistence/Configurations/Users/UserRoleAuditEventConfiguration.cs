using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Users;


internal sealed class UserRoleAuditEventConfiguration : IEntityTypeConfiguration<UserRoleAuditEvent> {
    public void Configure(EntityTypeBuilder<UserRoleAuditEvent> builder) {
        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.RoleId).HasConversion(
            id => id.Value,
            value => new RoleId(value));

        builder.Property(e => e.ActorUserId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            value => value.HasValue ? new UserId(value.Value) : null);

        builder.Property(e => e.RoleName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.Action)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.OccurredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.ActorUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ActorUserId);
        builder.HasIndex(e => e.RoleId);
        builder.HasIndex(e => e.RoleName);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.OccurredAtUtc);
    }
}
