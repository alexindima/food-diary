using FoodDiary.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FoodDiary.Infrastructure.Persistence.Converters;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Notifications;


internal sealed class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription> {
    public void Configure(EntityTypeBuilder<WebPushSubscription> builder) {
        builder.Property(e => e.Id)
            .HasConversion(StronglyTypedIdConverters.WebPushSubscriptionIdConverter.Instance)
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .HasConversion(StronglyTypedIdConverters.UserIdConverter.Instance);

        builder.Property(e => e.Endpoint)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(e => e.P256Dh)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Auth)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Locale)
            .HasMaxLength(16);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Endpoint)
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
