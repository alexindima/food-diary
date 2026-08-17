using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class CycleConsentConfiguration : IEntityTypeConfiguration<CycleConsent> {
    public void Configure(EntityTypeBuilder<CycleConsent> builder) {
        builder.ToTable("CycleConsents");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleConsentId(value));
        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));
        builder.Property(e => e.Purpose).HasConversion<string>().HasMaxLength(64);

        builder.HasIndex(e => new { e.CycleProfileId, e.Purpose }).IsUnique();
    }
}
