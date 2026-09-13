using FoodDiary.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.ContentReports.Infrastructure.Persistence;

public sealed class ContentReportsDbContext(DbContextOptions<ContentReportsDbContext> options) : DbContext(options) {
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyContentReportsPersistenceModel();
    }
}
