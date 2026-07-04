using FoodDiary.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<NutritionLesson> NutritionLessons => Set<NutritionLesson>();
    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();
}
