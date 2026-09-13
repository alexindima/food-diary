using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Exercises.Infrastructure.Persistence;

public sealed class ExercisesDbContext(DbContextOptions<ExercisesDbContext> options) : DbContext(options) {
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyExercisesPersistenceModel();
    }
}
