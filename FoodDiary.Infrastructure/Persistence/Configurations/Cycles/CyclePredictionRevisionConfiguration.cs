using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class CyclePredictionRevisionConfiguration : IEntityTypeConfiguration<CyclePredictionRevision> {
    public void Configure(EntityTypeBuilder<CyclePredictionRevision> builder) {
        builder.ToTable("CyclePredictionRevisions");
        builder.Property(revision => revision.Id).HasConversion(
            id => id.Value,
            value => new CyclePredictionRevisionId(value));
        builder.Property(revision => revision.CycleProfileId).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));
        builder.Property(revision => revision.NextPeriodStartFrom).HasColumnType("date");
        builder.Property(revision => revision.NextPeriodStartTo).HasColumnType("date");
        builder.Property(revision => revision.Confidence).HasMaxLength(32);
        builder.Property(revision => revision.DataSufficiency).HasMaxLength(32);
        builder.Property(revision => revision.PatternConsistency).HasMaxLength(32);
        builder.Property(revision => revision.ReasonCodes).HasMaxLength(512);
        builder.Property(revision => revision.AlgorithmVersion).HasMaxLength(64);
        builder.HasIndex(revision => new { revision.CycleProfileId, revision.GeneratedAtUtc });
    }
}
