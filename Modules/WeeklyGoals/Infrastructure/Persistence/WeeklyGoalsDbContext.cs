using FoodDiary.Modules.WeeklyGoals.PersistenceModel;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;

public sealed class WeeklyGoalsDbContext(DbContextOptions<WeeklyGoalsDbContext> options) : DbContext(options) {
    public DbSet<WeeklyGoal> WeeklyGoals => Set<WeeklyGoal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyWeeklyGoalsPersistenceModel();
}
