using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.DailyAdvices.PersistenceModel;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;

public sealed class DailyAdvicesDbContext(DbContextOptions<DailyAdvicesDbContext> options) : DbContext(options) {
    public DbSet<DailyAdvice> DailyAdvices => Set<DailyAdvice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyDailyAdvicesPersistenceModel();
    }
}
