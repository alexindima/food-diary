using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Runtime.Persistence;

/// <summary>Runtime storage for shared audit and delivery records, without module aggregates.</summary>
public abstract partial class SharedPersistenceDbContext : DbContext {
    protected SharedPersistenceDbContext(DbContextOptions options) : base(options) {
        Session = new PersistenceSession(this);
    }

    internal DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();
    internal DbSet<OutboxReplayAudit> OutboxReplayAudits => Set<OutboxReplayAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyAuditPersistenceModel();
        modelBuilder.ApplyEmailPersistenceModel();
        modelBuilder.ApplyOutboxPersistenceModel();
    }
}
