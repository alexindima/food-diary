using FoodDiary.Modules.Wearables.PersistenceModel;
using FoodDiary.Modules.Wearables.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Wearables.Infrastructure.Persistence;

public sealed class WearablesDbContext(DbContextOptions<WearablesDbContext> options) : DbContext(options) {
    public DbSet<WearableConnection> WearableConnections => Set<WearableConnection>();
    public DbSet<WearableSyncEntry> WearableSyncEntries => Set<WearableSyncEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyWearablesPersistenceModel();
}
