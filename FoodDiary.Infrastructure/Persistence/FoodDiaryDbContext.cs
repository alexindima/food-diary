using Microsoft.EntityFrameworkCore;
using FoodDiary.Infrastructure.Persistence.Audit;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : DbContext(options) {
    internal DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FoodDiaryDbContext).Assembly);
    }
}
