using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Identity.PersistenceModel.Configurations.Authentication;

internal sealed class ConsumedTelegramAssertionConfiguration : IEntityTypeConfiguration<ConsumedTelegramAssertion> {
    public void Configure(EntityTypeBuilder<ConsumedTelegramAssertion> builder) {
        builder.ToTable("ConsumedTelegramAssertions", table => table.HasCheckConstraint("CK_ConsumedTelegramAssertions_V2", "\"Fingerprint\" LIKE 'v2:%'"));
        builder.HasKey(e => e.Fingerprint);
        builder.Property(e => e.Fingerprint).HasMaxLength(67);
        builder.Property(e => e.ExpiresAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(e => e.ExpiresAtUtc);
    }
}
