using FoodDiary.Modules.Exercises.PersistenceModel;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Exercises.Infrastructure.Persistence;

public sealed class ExercisesDbContext(DbContextOptions<ExercisesDbContext> options) : DbContext(options) {
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyExercisesPersistenceModel();
    }
}
