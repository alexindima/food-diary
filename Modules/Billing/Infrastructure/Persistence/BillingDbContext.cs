using FoodDiary.Modules.Billing.PersistenceModel;
using FoodDiary.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options) {
    public DbSet<BillingSubscription> BillingSubscriptions => Set<BillingSubscription>();
    public DbSet<BillingPayment> BillingPayments => Set<BillingPayment>();
    public DbSet<BillingWebhookEvent> BillingWebhookEvents => Set<BillingWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyBillingPersistenceModel();
}
