using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Cycles;

internal sealed class CycleProfileConfiguration : IEntityTypeConfiguration<CycleProfile> {
    public void Configure(EntityTypeBuilder<CycleProfile> builder) {
        builder.ToTable("CycleProfiles");

        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleProfileId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.Mode).HasConversion<string>().HasMaxLength(64);
        builder.Property(e => e.Confidence).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.TrackingStartDate).HasColumnType("date");
        builder.Property(e => e.AverageCycleLength).HasDefaultValue(28);
        builder.Property(e => e.AveragePeriodLength).HasDefaultValue(5);
        builder.Property(e => e.LutealLength).HasDefaultValue(14);
        builder.Property(e => e.Notes).HasMaxLength(1024);

        builder.HasOne<global::FoodDiary.Domain.Entities.Users.User>()
            .WithMany(u => u.Cycles)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Factors)
            .WithOne(e => e.CycleProfile)
            .HasForeignKey(e => e.CycleProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.BleedingEntries)
            .WithOne(e => e.CycleProfile)
            .HasForeignKey(e => e.CycleProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SymptomEntries)
            .WithOne(e => e.CycleProfile)
            .HasForeignKey(e => e.CycleProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.FertilitySignals)
            .WithOne(e => e.CycleProfile)
            .HasForeignKey(e => e.CycleProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Factors).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.BleedingEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.SymptomEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.FertilitySignals).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.UserId).IsUnique();
    }
}
