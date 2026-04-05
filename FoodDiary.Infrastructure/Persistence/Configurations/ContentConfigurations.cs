using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;

internal sealed class NutritionLessonConfiguration : IEntityTypeConfiguration<NutritionLesson> {
    public void Configure(EntityTypeBuilder<NutritionLesson> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new NutritionLessonId(value));

        entity.Property(e => e.Title).HasMaxLength(256);
        entity.Property(e => e.Content).HasMaxLength(8192);
        entity.Property(e => e.Summary).HasMaxLength(512);
        entity.Property(e => e.Locale).HasMaxLength(10);

        entity.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(16);

        entity.HasIndex(e => new { e.Locale, e.Category });
    }
}

internal sealed class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress> {
    public void Configure(EntityTypeBuilder<UserLessonProgress> entity) {
        entity.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new UserLessonProgressId(value));

        entity.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        entity.Property(e => e.LessonId).HasConversion(
            id => id.Value,
            value => new NutritionLessonId(value));

        entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Lesson)
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
