using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification> {
    public void Configure(EntityTypeBuilder<Notification> entity) {
        entity.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new NotificationId(value))
            .ValueGeneratedNever();

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.Body)
            .HasMaxLength(1000);

        entity.Property(e => e.ReferenceId)
            .HasMaxLength(128);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => new { e.UserId, e.IsRead });
        entity.HasIndex(e => e.UserId);
    }
}
