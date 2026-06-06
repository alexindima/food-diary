using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class BillingWebhookEventConfiguration : IEntityTypeConfiguration<BillingWebhookEvent> {
    public void Configure(EntityTypeBuilder<BillingWebhookEvent> builder) {
        builder.ToTable("BillingWebhookEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.ExternalObjectId)
            .HasMaxLength(255);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.PayloadJson)
            .HasColumnType("jsonb");

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.Provider, e.EventId })
            .IsUnique();

        builder.HasIndex(e => e.ProcessedAtUtc);
    }
}
