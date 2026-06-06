using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class CycleDayConfiguration : IEntityTypeConfiguration<CycleDay> {
    public void Configure(EntityTypeBuilder<CycleDay> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new CycleDayId(value));

        builder.Property(e => e.CycleId).HasConversion(
            id => id.Value,
            value => new CycleId(value));

        builder.Property(e => e.Date)
            .HasColumnType("date");

        builder.Property(e => e.Notes)
            .HasMaxLength(1024);

        builder.HasIndex(e => new { e.CycleId, e.Date }).IsUnique();

        builder.OwnsOne(e => e.Symptoms, builder => {
            builder.Property(s => s.Pain).HasColumnName("Pain").IsRequired();
            builder.Property(s => s.Mood).HasColumnName("Mood").IsRequired();
            builder.Property(s => s.Edema).HasColumnName("Edema").IsRequired();
            builder.Property(s => s.Headache).HasColumnName("Headache").IsRequired();
            builder.Property(s => s.Energy).HasColumnName("Energy").IsRequired();
            builder.Property(s => s.SleepQuality).HasColumnName("SleepQuality").IsRequired();
            builder.Property(s => s.Libido).HasColumnName("Libido").IsRequired();
        });
    }
}
