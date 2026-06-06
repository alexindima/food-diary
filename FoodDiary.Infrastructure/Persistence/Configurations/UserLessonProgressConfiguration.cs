using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Infrastructure.Persistence.Configurations;


internal sealed class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress> {
    public void Configure(EntityTypeBuilder<UserLessonProgress> builder) {
        builder.Property(e => e.Id).HasConversion(
            id => id.Value,
            value => new UserLessonProgressId(value));

        builder.Property(e => e.UserId).HasConversion(
            id => id.Value,
            value => new UserId(value));

        builder.Property(e => e.LessonId).HasConversion(
            id => id.Value,
            value => new NutritionLessonId(value));

        builder.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Lesson)
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
