using FoodDiary.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public sealed class HydrationDbContext(DbContextOptions<HydrationDbContext> options) : DbContext(options) {
    public DbSet<HydrationEntry> HydrationEntries => Set<HydrationEntry>();
    public DbSet<HydrationOperationReceipt> HydrationOperationReceipts => Set<HydrationOperationReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyHydrationPersistenceModel();
    }
}
