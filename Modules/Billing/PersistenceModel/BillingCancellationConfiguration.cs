using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Billing.PersistenceModel;

internal sealed class BillingCancellationConfiguration : IEntityTypeConfiguration<BillingCancellation> {
    public void Configure(EntityTypeBuilder<BillingCancellation> builder) {
        builder.ToTable("BillingCancellations");
        builder.HasKey(item => item.Id);
        builder.Ignore(item => item.TypedId);
        builder.Property(item => item.UserId).HasConversion(id => id.Value, value => new UserId(value));
        builder.Property(item => item.Provider).IsRequired().HasMaxLength(32);
        builder.Property(item => item.ExternalSubscriptionId).IsRequired().HasMaxLength(255);
        builder.Property(item => item.Plan).HasMaxLength(32);
        builder.Property(item => item.Source).IsRequired().HasMaxLength(32);
        builder.Property(item => item.Outcome).IsRequired().HasMaxLength(32);
        builder.Property(item => item.ReasonCode).HasMaxLength(64);
        builder.Property(item => item.Comment).HasMaxLength(2000);
        builder.Property(item => item.RequestedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.EffectiveAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.FeedbackSubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.ConfirmationEmailHandledAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.ConfirmationTelegramHandledAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(item => new { item.ConfirmationTelegramHandledAtUtc, item.RequestedAtUtc })
            .HasDatabaseName("IX_BillingCancellations_TelegramPending")
            .HasFilter("\"ConfirmationTelegramHandledAtUtc\" IS NULL AND \"Provider\" = 'TelegramStars'");
        builder.HasIndex(item => new { item.ConfirmationEmailHandledAtUtc, item.RequestedAtUtc })
            .HasFilter("\"ConfirmationEmailHandledAtUtc\" IS NULL");
        builder.HasIndex(item => new { item.UserId, item.Provider, item.ExternalSubscriptionId, item.RequestedAtUtc });
        builder.HasIndex(item => item.RequestedAtUtc);
    }
}
