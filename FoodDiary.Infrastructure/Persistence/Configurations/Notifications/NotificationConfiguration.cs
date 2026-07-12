using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Notifications;


internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification> {
    public void Configure(EntityTypeBuilder<Notification> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new NotificationId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Type)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.PayloadJson)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.ReferenceId)
            .HasMaxLength(128);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => e.UserId);
    }
}
