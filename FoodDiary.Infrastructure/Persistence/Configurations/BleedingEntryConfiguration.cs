using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class BleedingEntryConfiguration : IEntityTypeConfiguration<BleedingEntry> {
    public void Configure(EntityTypeBuilder<BleedingEntry> builder) {
        builder.ToTable("CycleBleedingEntries");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new BleedingEntryId(value));

        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.Date).HasColumnType("date");
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Flow).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Notes).HasMaxLength(1024);

        builder.HasIndex(e => new { e.CycleProfileId, e.Date, e.Type }).IsUnique();
    }
}
