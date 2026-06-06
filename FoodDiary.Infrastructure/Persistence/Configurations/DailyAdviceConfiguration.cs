using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class DailyAdviceConfiguration : IEntityTypeConfiguration<DailyAdvice> {
    public void Configure(EntityTypeBuilder<DailyAdvice> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new DailyAdviceId(value));

        builder.Property(e => e.Locale)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Value)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Tag)
            .HasMaxLength(64);

        builder.Property(e => e.Weight)
            .HasDefaultValue(1);

        builder.HasIndex(e => new { e.Locale, e.Tag });
    }
}
