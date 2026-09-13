using FoodDiary.Modules.Admin.PersistenceModel;
using FoodDiary.Modules.Admin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Admin.Infrastructure.Persistence;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options) {
    public DbSet<AdminImpersonationSession> AdminImpersonationSessions => Set<AdminImpersonationSession>();
    public DbSet<BugAcknowledgementReceipt> BugAcknowledgementReceipts => Set<BugAcknowledgementReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyAdminPersistenceModel();
    }
}
