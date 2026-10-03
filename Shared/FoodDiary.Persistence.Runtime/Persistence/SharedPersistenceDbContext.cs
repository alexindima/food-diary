using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Audit.PersistenceModel;
using FoodDiary.Email.PersistenceModel;
using FoodDiary.Outbox.PersistenceModel;
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
        modelBuilder.ApplyConfiguration(new AtomicCommandReceiptConfiguration());
    }
}
