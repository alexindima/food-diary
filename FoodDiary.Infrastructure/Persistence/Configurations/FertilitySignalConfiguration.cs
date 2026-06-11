using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class FertilitySignalConfiguration : IEntityTypeConfiguration<FertilitySignal> {
    public void Configure(EntityTypeBuilder<FertilitySignal> builder) {
        builder.ToTable("FertilitySignals");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new FertilitySignalId(value));

        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.Date).HasColumnType("date");
        builder.Property(e => e.OvulationTestResult).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.CervicalFluid).HasMaxLength(128);
        builder.Property(e => e.Notes).HasMaxLength(1024);

        builder.HasIndex(e => new { e.CycleProfileId, e.Date }).IsUnique();
    }
}
