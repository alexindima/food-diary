using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class BillingSubscriptionConfiguration : IEntityTypeConfiguration<BillingSubscription> {
    public void Configure(EntityTypeBuilder<BillingSubscription> entity) {
        entity.ToTable("BillingSubscriptions");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.ExternalCustomerId)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.ExternalSubscriptionId)
            .HasMaxLength(255);

        entity.Property(e => e.ExternalPriceId)
            .HasMaxLength(255);

        entity.Property(e => e.Plan)
            .HasMaxLength(32);

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.CurrentPeriodStartUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.CurrentPeriodEndUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.CanceledAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.TrialStartUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.TrialEndUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.LastWebhookEventId)
            .HasMaxLength(255);

        entity.Property(e => e.LastSyncedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.HasIndex(e => e.UserId)
            .IsUnique();

        entity.HasIndex(e => new { e.Provider, e.ExternalCustomerId })
            .IsUnique();

        entity.HasIndex(e => new { e.Provider, e.ExternalSubscriptionId })
            .IsUnique();

        entity.HasOne<Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
