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

        entity.Property(e => e.ExternalPaymentMethodId)
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

        entity.Property(e => e.NextBillingAttemptUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.ProviderMetadataJson)
            .HasColumnType("jsonb");

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

        entity.HasIndex(e => new { e.Provider, e.ExternalPaymentMethodId });

        entity.HasOne<Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BillingPaymentConfiguration : IEntityTypeConfiguration<BillingPayment> {
    public void Configure(EntityTypeBuilder<BillingPayment> entity) {
        entity.ToTable("BillingPayments");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.ExternalPaymentId)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.ExternalCustomerId)
            .HasMaxLength(255);

        entity.Property(e => e.ExternalSubscriptionId)
            .HasMaxLength(255);

        entity.Property(e => e.ExternalPaymentMethodId)
            .HasMaxLength(255);

        entity.Property(e => e.ExternalPriceId)
            .HasMaxLength(255);

        entity.Property(e => e.Plan)
            .HasMaxLength(32);

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Kind)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.Amount)
            .HasPrecision(18, 2);

        entity.Property(e => e.Currency)
            .HasMaxLength(3);

        entity.Property(e => e.CurrentPeriodStartUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.CurrentPeriodEndUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.WebhookEventId)
            .HasMaxLength(255);

        entity.Property(e => e.ProviderMetadataJson)
            .HasColumnType("jsonb");

        entity.HasIndex(e => new { e.Provider, e.ExternalPaymentId });

        entity.HasIndex(e => new { e.Provider, e.ExternalCustomerId });

        entity.HasIndex(e => new { e.Provider, e.ExternalSubscriptionId });

        entity.HasIndex(e => new { e.Provider, e.ExternalPaymentMethodId });

        entity.HasIndex(e => e.UserId);

        entity.HasOne<Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<BillingSubscription>()
            .WithMany()
            .HasForeignKey(e => e.BillingSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class BillingWebhookEventConfiguration : IEntityTypeConfiguration<BillingWebhookEvent> {
    public void Configure(EntityTypeBuilder<BillingWebhookEvent> entity) {
        entity.ToTable("BillingWebhookEvents");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(255);

        entity.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.ExternalObjectId)
            .HasMaxLength(255);

        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        entity.Property(e => e.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone");

        entity.Property(e => e.PayloadJson)
            .HasColumnType("jsonb");

        entity.Property(e => e.ErrorMessage)
            .HasMaxLength(1024);

        entity.HasIndex(e => new { e.Provider, e.EventId })
            .IsUnique();

        entity.HasIndex(e => e.ProcessedAtUtc);
    }
}
