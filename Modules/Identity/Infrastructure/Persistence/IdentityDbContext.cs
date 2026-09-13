using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Authentication;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options) {
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<UserLoginEvent> UserLoginEvents => Set<UserLoginEvent>();
    public DbSet<UserRefreshTokenSession> UserRefreshTokenSessions => Set<UserRefreshTokenSession>();
    public DbSet<ConsumedTelegramAssertion> ConsumedTelegramAssertions => Set<ConsumedTelegramAssertion>();
    public DbSet<TelegramLoginTicket> TelegramLoginTickets => Set<TelegramLoginTicket>();
    public DbSet<TelegramOperation> TelegramOperations => Set<TelegramOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyIdentityPersistenceModel();
}
