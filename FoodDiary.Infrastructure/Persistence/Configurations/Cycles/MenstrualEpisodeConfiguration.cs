using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class MenstrualEpisodeConfiguration : IEntityTypeConfiguration<MenstrualEpisode> {
    public void Configure(EntityTypeBuilder<MenstrualEpisode> builder) {
        builder.ToTable("CycleMenstrualEpisodes");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new MenstrualEpisodeId(value));

        builder.Property(e => e.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.StartDate).HasColumnType("date");
        builder.Property(e => e.EndDate).HasColumnType("date");
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(e => new { e.CycleProfileId, e.StartDate }).IsUnique();
    }
}
