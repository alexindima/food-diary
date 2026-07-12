using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class CycleFactorConfiguration : IEntityTypeConfiguration<CycleFactor> {
    public void Configure(EntityTypeBuilder<CycleFactor> builder) {
        builder.ToTable("CycleFactors");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleFactorId(value));

        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(64);
        builder.Property(e => e.StartDate).HasColumnType("date");
        builder.Property(e => e.EndDate).HasColumnType("date");
        builder.Property(e => e.Notes).HasMaxLength(1024);

        builder.HasIndex(e => new { e.CycleProfileId, e.Type, e.StartDate }).IsUnique();
    }
}
