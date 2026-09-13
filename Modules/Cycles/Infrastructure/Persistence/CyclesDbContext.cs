using FoodDiary.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Cycles.Infrastructure.Persistence;

public sealed class CyclesDbContext(DbContextOptions<CyclesDbContext> options) : DbContext(options) {
    public DbSet<CycleProfile> CycleProfiles => Set<CycleProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyCyclesPersistenceModel();
    }
}
