using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FoodDiary.Infrastructure.Persistence.Converters;

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

        entity.Property(e => e.PayloadJson)
            .IsRequired()
            .HasMaxLength(4000);

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

internal sealed class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription> {
    public void Configure(EntityTypeBuilder<WebPushSubscription> entity) {
        entity.Property(e => e.Id)
            .HasConversion(StronglyTypedIdConverters.WebPushSubscriptionIdConverter.Instance)
            .ValueGeneratedNever();

        entity.Property(e => e.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        entity.Property(e => e.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        entity.Property(e => e.P256Dh)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.Auth)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(e => e.Locale)
            .HasMaxLength(16);

        entity.Property(e => e.UserAgent)
            .HasMaxLength(512);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(e => e.Endpoint)
            .IsUnique();

        entity.HasIndex(e => e.UserId);
    }
}
