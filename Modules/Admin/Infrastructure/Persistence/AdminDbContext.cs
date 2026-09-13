using FoodDiary.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options) {
    public DbSet<AdminImpersonationSession> AdminImpersonationSessions => Set<AdminImpersonationSession>();
    public DbSet<BugAcknowledgementReceipt> BugAcknowledgementReceipts => Set<BugAcknowledgementReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyAdminPersistenceModel();
    }
}
