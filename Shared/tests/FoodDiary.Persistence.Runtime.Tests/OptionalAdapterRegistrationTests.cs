using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Persistence.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class OptionalAdapterRegistrationTests {
    [Fact]
    public async Task AuditAndEmailEnqueueIntoTheSameScopeWithoutSavingAsync() {
        var services = new ServiceCollection();
        services.AddScoped<SharedPersistenceDbContext>(_ => new SharedRuntimeDbContext(
            new DbContextOptionsBuilder<SharedPersistenceDbContext>()
                .UseNpgsql("Host=localhost;Database=adapter_model;Username=test").Options));
        services.AddAuditInfrastructure().AddEmailInfrastructure();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope first = provider.CreateAsyncScope();
        await using AsyncServiceScope second = provider.CreateAsyncScope();
        IAuditEntryWriter audit = first.ServiceProvider.GetRequiredService<IAuditEntryWriter>();
        Assert.Same(audit, first.ServiceProvider.GetRequiredService<IAuditEntryJournal>());
        Assert.Same(audit, first.ServiceProvider.GetRequiredService<IAuditEntryReadService>());
        await audit.AddAsync(new UserId(Guid.NewGuid()), subjectClientUserId: null, "test", "test", targetId: null, metadata: null, CancellationToken.None);
        await first.ServiceProvider.GetRequiredService<IEmailOutbox>().EnqueueAsync(
            new EmailMessage("sender@example.com", "Sender", ["recipient@example.com"], "Test", "<p>Test</p>", "Test"), CancellationToken.None);

        SharedPersistenceDbContext context = first.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        Assert.Equal(["AuditEntry", "EmailOutboxMessage"], context.ChangeTracker.Entries()
            .Select(entry => entry.Metadata.ClrType.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
        Assert.All(context.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Added, entry.State));
        Assert.Empty(second.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>().ChangeTracker.Entries());
    }
}
