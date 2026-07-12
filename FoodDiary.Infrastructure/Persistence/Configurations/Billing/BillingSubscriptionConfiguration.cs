using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Billing;


internal sealed class BillingSubscriptionConfiguration : IEntityTypeConfiguration<BillingSubscription> {
    public void Configure(EntityTypeBuilder<BillingSubscription> builder) {
        builder.ToTable("BillingSubscriptions");

        builder.HasKey(e => e.Id);

        ConfigureIdentifiers(builder);
        ConfigureStatusAndPeriods(builder);
        ConfigureIndexes(builder);

        builder.HasOne<Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIdentifiers(EntityTypeBuilder<BillingSubscription> builder) {
        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ExternalCustomerId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ExternalSubscriptionId)
            .HasMaxLength(255);

        builder.Property(e => e.ExternalPaymentMethodId)
            .HasMaxLength(255);

        builder.Property(e => e.ExternalPriceId)
            .HasMaxLength(255);

        builder.Property(e => e.Plan)
            .HasMaxLength(32);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(64);
    }

    private static void ConfigureStatusAndPeriods(EntityTypeBuilder<BillingSubscription> builder) {
        builder.Property(e => e.CurrentPeriodStartUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CurrentPeriodEndUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CanceledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.TrialStartUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.TrialEndUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.NextBillingAttemptUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ProviderMetadataJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.LastWebhookEventId)
            .HasMaxLength(255);

        builder.Property(e => e.LastSyncedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.PremiumRoleManagedByBilling)
            .HasDefaultValue(value: false);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<BillingSubscription> builder) {
        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasIndex(e => new { e.Provider, e.ExternalCustomerId })
            .IsUnique();

        builder.HasIndex(e => new { e.Provider, e.ExternalSubscriptionId })
            .IsUnique();

        builder.HasIndex(e => new { e.Provider, e.ExternalPaymentMethodId });
    }
}
