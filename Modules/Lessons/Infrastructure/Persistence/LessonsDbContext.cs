using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Lessons.Infrastructure.Persistence;

public sealed class LessonsDbContext(DbContextOptions<LessonsDbContext> options) : DbContext(options) {
    public DbSet<NutritionLesson> NutritionLessons => Set<NutritionLesson>();
    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyLessonsPersistenceModel();
    }
}
