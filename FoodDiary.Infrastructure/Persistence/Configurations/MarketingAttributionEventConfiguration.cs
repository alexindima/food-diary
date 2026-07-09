using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class MarketingAttributionEventConfiguration : IEntityTypeConfiguration<MarketingAttributionEvent> {
    public void Configure(EntityTypeBuilder<MarketingAttributionEvent> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MarketingAttributionEventId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.AnonymousId)
            .IsRequired()
            .HasMaxLength(96);

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasMaxLength(96);

        builder.Property(e => e.LandingPath)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ReferrerHost)
            .HasMaxLength(128);

        builder.Property(e => e.UtmSource)
            .HasMaxLength(160);

        builder.Property(e => e.UtmMedium)
            .HasMaxLength(160);

        builder.Property(e => e.UtmCampaign)
            .HasMaxLength(160);

        builder.Property(e => e.UtmContent)
            .HasMaxLength(160);

        builder.Property(e => e.UtmTerm)
            .HasMaxLength(160);

        builder.Property(e => e.BuildVersion)
            .HasMaxLength(64);

        builder.Property(e => e.OccurredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => new { e.AnonymousId, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.SessionId, e.OccurredAtUtc });
        builder.HasIndex(e => new { e.UtmSource, e.UtmMedium, e.UtmCampaign, e.OccurredAtUtc });
    }
}
