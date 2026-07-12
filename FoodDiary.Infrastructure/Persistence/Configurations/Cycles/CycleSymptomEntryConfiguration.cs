using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class CycleSymptomEntryConfiguration : IEntityTypeConfiguration<CycleSymptomEntry> {
    public void Configure(EntityTypeBuilder<CycleSymptomEntry> builder) {
        builder.ToTable("CycleSymptomEntries");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleSymptomEntryId(value));

        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.Date).HasColumnType("date");
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(64);
        builder.Property(e => e.TagsJson).HasColumnType("jsonb");
        builder.Property(e => e.Note).HasMaxLength(1024);

        builder.Ignore(e => e.Tags);
        builder.HasIndex(e => new { e.CycleProfileId, e.Date, e.Category }).IsUnique();
    }
}
