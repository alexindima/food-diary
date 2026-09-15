using FoodDiary.Modules.Lessons.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodDiary.Modules.Lessons.PersistenceModel.Configurations;

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

        builder.HasOne(e => e.Lesson)
            .WithMany()
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
