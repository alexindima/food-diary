using FoodDiary.Modules.Cycles.PersistenceModel;
using FoodDiary.Modules.Cycles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Cycles.Infrastructure.Persistence;

public sealed class CyclesDbContext(DbContextOptions<CyclesDbContext> options) : DbContext(options) {
    public DbSet<CycleProfile> CycleProfiles => Set<CycleProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyCyclesPersistenceModel();
    }
}
