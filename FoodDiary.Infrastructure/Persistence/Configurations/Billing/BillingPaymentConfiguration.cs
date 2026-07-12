using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Billing;


internal sealed class BillingPaymentConfiguration : IEntityTypeConfiguration<BillingPayment> {
    public void Configure(EntityTypeBuilder<BillingPayment> builder) {
        builder.ToTable("BillingPayments");

        builder.HasKey(e => e.Id);

        ConfigureIdentifiers(builder);
        ConfigurePaymentDetails(builder);
        ConfigureIndexes(builder);

        builder.HasOne<Domain.Entities.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<BillingSubscription>()
            .WithMany()
            .HasForeignKey(e => e.BillingSubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureIdentifiers(EntityTypeBuilder<BillingPayment> builder) {
        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ExternalPaymentId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ExternalCustomerId)
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

    private static void ConfigurePaymentDetails(EntityTypeBuilder<BillingPayment> builder) {
        builder.Property(e => e.Kind)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(3);

        builder.Property(e => e.CurrentPeriodStartUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CurrentPeriodEndUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.WebhookEventId)
            .HasMaxLength(255);

        builder.Property(e => e.ProviderMetadataJson)
            .HasColumnType("jsonb");
    }

    private static void ConfigureIndexes(EntityTypeBuilder<BillingPayment> builder) {
        builder.HasIndex(e => new { e.Provider, e.ExternalPaymentId })
            .IsUnique();

        builder.HasIndex(e => new { e.Provider, e.ExternalCustomerId });

        builder.HasIndex(e => new { e.Provider, e.ExternalSubscriptionId });

        builder.HasIndex(e => new { e.Provider, e.ExternalPaymentMethodId });

        builder.HasIndex(e => e.UserId);
    }
}
