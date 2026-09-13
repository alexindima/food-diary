using FoodDiary.Domain.Entities.Meals;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class MealsDbContext(DbContextOptions<MealsDbContext> options) : DbContext(options) {
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<MealAiSession> MealAiSessions => Set<MealAiSession>();
    public DbSet<MealAiItem> MealAiItems => Set<MealAiItem>();
    public DbSet<MealRecognitionReceipt> MealRecognitionReceipts => Set<MealRecognitionReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyMealsPersistenceModel();
}
