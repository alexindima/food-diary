using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class NotificationWebPushOutboxMessageConfiguration : IEntityTypeConfiguration<NotificationWebPushOutboxMessage> {
    public void Configure(EntityTypeBuilder<NotificationWebPushOutboxMessage> builder) {
        builder.ToTable("NotificationWebPushOutbox");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.NotificationId)
            .HasConversion(
                id => id.Value,
                value => new NotificationId(value))
            .ValueGeneratedNever();

        builder.Property(message => message.LastError)
            .HasMaxLength(2048);

        builder.HasOne(message => message.Notification)
            .WithMany()
            .HasForeignKey(message => message.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => new { message.ProcessedOnUtc, message.NextAttemptOnUtc });
        builder.HasIndex(message => message.NotificationId);
    }
}
