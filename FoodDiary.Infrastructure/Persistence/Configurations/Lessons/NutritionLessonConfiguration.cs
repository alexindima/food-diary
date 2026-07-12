using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations.Lessons;


internal sealed class NutritionLessonConfiguration : IEntityTypeConfiguration<NutritionLesson> {
    public void Configure(EntityTypeBuilder<NutritionLesson> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new NutritionLessonId(value));

        builder.Property(e => e.Title).HasMaxLength(256);
        builder.Property(e => e.Content).HasMaxLength(65536);
        builder.Property(e => e.Summary).HasMaxLength(512);
        builder.Property(e => e.Locale).HasMaxLength(10);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.HasIndex(e => new { e.Locale, e.Category });
    }
}
