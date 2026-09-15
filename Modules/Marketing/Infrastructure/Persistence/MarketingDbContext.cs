using FoodDiary.Modules.Marketing.PersistenceModel;
using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Marketing.Infrastructure.Persistence;

public sealed class MarketingDbContext(DbContextOptions<MarketingDbContext> options) : DbContext(options) {
    public DbSet<MarketingAttributionEvent> MarketingAttributionEvents => Set<MarketingAttributionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyMarketingPersistenceModel();
    }
}
