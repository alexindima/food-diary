using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class MealAiItemConfiguration : IEntityTypeConfiguration<MealAiItem> {
    public void Configure(EntityTypeBuilder<MealAiItem> builder) {
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => new MealAiItemId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.MealAiSessionId).HasConversion(
            id => id.Value,
            value => new MealAiSessionId(value));

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NameLocal)
            .HasMaxLength(256);

        builder.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Confidence)
            .HasDefaultValue(1d);

        builder.Property(e => e.Resolution)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(FoodDiary.Domain.Enums.MealAiItemResolution.Accepted);
    }
}
