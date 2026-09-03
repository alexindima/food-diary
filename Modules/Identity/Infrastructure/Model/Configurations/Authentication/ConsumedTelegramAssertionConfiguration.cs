using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Authentication;

internal sealed class ConsumedTelegramAssertionConfiguration : IEntityTypeConfiguration<ConsumedTelegramAssertion> {
    public void Configure(EntityTypeBuilder<ConsumedTelegramAssertion> builder) {
        builder.ToTable("ConsumedTelegramAssertions");
        builder.HasKey(e => e.Fingerprint);
        builder.Property(e => e.Fingerprint).HasMaxLength(64);
        builder.Property(e => e.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(e => e.ExpiresAtUtc);
    }
}
