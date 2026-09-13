using FoodDiary.Domain.Entities.Recents;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class RecentItemsDbContext(DbContextOptions<RecentItemsDbContext> options) : DbContext(options) {
    public DbSet<RecentItem> RecentItems => Set<RecentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyRecentItemsPersistenceModel();
    }
}
