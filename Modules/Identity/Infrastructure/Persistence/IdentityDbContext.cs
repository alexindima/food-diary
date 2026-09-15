using FoodDiary.Modules.Identity.PersistenceModel;
using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options) {
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<UserLoginEvent> UserLoginEvents => Set<UserLoginEvent>();
    public DbSet<UserRefreshTokenSession> UserRefreshTokenSessions => Set<UserRefreshTokenSession>();
    public DbSet<ConsumedTelegramAssertion> ConsumedTelegramAssertions => Set<ConsumedTelegramAssertion>();
    public DbSet<TelegramLoginTicket> TelegramLoginTickets => Set<TelegramLoginTicket>();
    public DbSet<TelegramOperation> TelegramOperations => Set<TelegramOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyIdentityPersistenceModel();
}
