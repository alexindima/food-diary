using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Modules.Billing.Infrastructure;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed partial class SharedBillingContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SharedSavePersistsUserAndOwnerRecordsAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        BillingDbContext owned = provider.GetRequiredService<BillingDbContext>();
        Assert.Multiple(
            () => Assert.Same(provider.GetRequiredService<IBillingSubscriptionReadModelRepository>(), provider.GetRequiredService<IBillingSubscriptionWriteRepository>()),
            () => Assert.Same(provider.GetRequiredService<IBillingPaymentReadRepository>(), provider.GetRequiredService<IBillingPaymentWriteRepository>()),
            () => Assert.Same(provider.GetRequiredService<IBillingWebhookEventReadRepository>(), provider.GetRequiredService<IBillingWebhookEventWriteRepository>()),
            () => Assert.False(context.Database.HasPendingModelChanges()));
        Assert.Equal(3, owned.Model.GetEntityTypes().Count());
        Assert.Same(context.Database.GetDbConnection(), owned.Database.GetDbConnection());
        var user = User.Create("billing-owner@example.com", "hash");
        context.Users.Add(user);
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.Stripe, "customer", externalPriceId: null, plan: null);
        await provider.GetRequiredService<IBillingSubscriptionWriteRepository>().AddAsync(subscription);
        await provider.GetRequiredService<IBillingPaymentWriteRepository>().AddAsync(CreatePayment(user.Id, "payment", subscription.Id));
        await provider.GetRequiredService<IBillingWebhookEventWriteRepository>().AddAsync(CreateWebhook("event"));
        Assert.Empty(context.ChangeTracker.Entries<BillingPayment>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        Assert.Single(await verification.BillingSubscriptions.ToListAsync());
        Assert.Equal(subscription.Id, (await verification.BillingPayments.SingleAsync()).BillingSubscriptionId);
        Assert.Single(await verification.BillingWebhookEvents.ToListAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateOwnerRecordRollsBackSharedWriteAndScopeCanBeReusedAsync(bool webhook) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("billing-duplicate@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IBillingPaymentWriteRepository payments = provider.GetRequiredService<IBillingPaymentWriteRepository>();
        IBillingWebhookEventWriteRepository events = provider.GetRequiredService<IBillingWebhookEventWriteRepository>();
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        await runner.ExecuteAsync(async token => {
            if (webhook) { await events.AddAsync(CreateWebhook("duplicate"), token); } else { await payments.AddAsync(CreatePayment(user.Id, "duplicate"), token); }
        });
        var rolledBackUser = User.Create("billing-rollback@example.com", "hash");
        Task AttemptAsync() => runner.ExecuteAsync(async token => {
            context.Users.Add(rolledBackUser);
            if (webhook) { await events.AddAsync(CreateWebhook("duplicate"), token); } else { await payments.AddAsync(CreatePayment(user.Id, "duplicate"), token); }
        });
        if (webhook) { await Assert.ThrowsAsync<BillingWebhookEventAlreadyProcessedException>(AttemptAsync); } else { await Assert.ThrowsAsync<BillingPaymentAlreadyExistsException>(AttemptAsync); }
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(provider.GetRequiredService<BillingDbContext>().ChangeTracker.Entries());
        Assert.False(await context.Users.AnyAsync(candidate => candidate.Id == rolledBackUser.Id));
        await runner.ExecuteAsync(async token => await payments.AddAsync(CreatePayment(user.Id, "retry"), token));
        Assert.NotNull(await provider.GetRequiredService<IBillingPaymentReadRepository>().GetByExternalPaymentIdAsync(BillingProviderNames.Stripe, "retry"));
    }

    [RequiresDockerFact]
    public async Task SaveThenReadAndRollbackDoesNotLeaveOwnerTransactionAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IBillingWebhookEventWriteRepository events = provider.GetRequiredService<IBillingWebhookEventWriteRepository>();
        IBillingWebhookEventReadRepository reads = provider.GetRequiredService<IBillingWebhookEventReadRepository>();
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync(async token => {
            await events.AddAsync(CreateWebhook("rollback"), token);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            Assert.True(await reads.ExistsAsync(BillingProviderNames.Stripe, "rollback", token));
            throw new InvalidOperationException("Injected failure after owner save.");
        }));
        Assert.False(await reads.ExistsAsync(BillingProviderNames.Stripe, "rollback"));
        await runner.ExecuteAsync(async token => await events.AddAsync(CreateWebhook("rollback"), token));
        Assert.True(await reads.ExistsAsync(BillingProviderNames.Stripe, "rollback"));
    }

    [RequiresDockerFact]
    public async Task SerializedOwnerReadsPreventConcurrentDuplicateEventsAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider firstProvider = CreateProvider(context);
        await using ServiceProvider secondProvider = CreateProvider(second);
        await Task.WhenAll(RegisterAsync(firstProvider), RegisterAsync(secondProvider));
        Assert.Equal(1, await context.BillingWebhookEvents.CountAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedAttemptPreservesExceptionAndResetsOwnedStateAsync(bool canceled) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(context);
        BillingDbContext owned = provider.GetRequiredService<BillingDbContext>();
        IBillingTransactionRunner runner = provider.GetRequiredService<IBillingTransactionRunner>();
        using var cancellation = new CancellationTokenSource();
        Exception failure = canceled ? new OperationCanceledException(cancellation.Token) : new InvalidOperationException("Injected failure.");
        Exception? actual = await Record.ExceptionAsync(() => runner.ExecuteAsync(async token => {
            context.Users.Add(User.Create("failed-attempt@example.com", "hash"));
            await provider.GetRequiredService<IBillingWebhookEventWriteRepository>().AddAsync(CreateWebhook("failed-attempt"), token);
            if (canceled) { await cancellation.CancelAsync(); }
            throw failure;
        }, cancellation.Token));
        User[] users = await context.Users.AsNoTracking().ToArrayAsync();
        BillingWebhookEvent[] events = await context.BillingWebhookEvents.AsNoTracking().ToArrayAsync();
        Assert.Multiple(
            () => Assert.Same(failure, actual),
            () => Assert.Empty(context.ChangeTracker.Entries()),
            () => Assert.Empty(owned.ChangeTracker.Entries()),
            () => Assert.Empty(users),
            () => Assert.Empty(events));
        await runner.ExecuteAsync(async token => await provider.GetRequiredService<IBillingWebhookEventWriteRepository>().AddAsync(CreateWebhook("next-attempt"), token));
        Assert.Single(await context.BillingWebhookEvents.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task EmptyBillingCommandStillInvokesUnitOfWorkOnceAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var coordinator = new FoodDiary.Persistence.Runtime.Persistence.Shared.EfModuleTransactionCoordinator(context, unitOfWork);
        var runner = new EfBillingTransactionRunner(coordinator);
        await runner.ExecuteAsync(_ => Task.CompletedTask);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static async Task RegisterAsync(ServiceProvider provider) {
        IBillingWebhookEventReadRepository reads = provider.GetRequiredService<IBillingWebhookEventReadRepository>();
        IBillingWebhookEventWriteRepository writes = provider.GetRequiredService<IBillingWebhookEventWriteRepository>();
        await provider.GetRequiredService<IBillingTransactionRunner>().ExecuteSerializedAsync("billing:shared-event", async token => {
            if (!await reads.ExistsAsync(BillingProviderNames.Stripe, "shared-event", token)) {
                await writes.AddAsync(CreateWebhook("shared-event"), token);
            }
        });
    }

    private static BillingWebhookEvent CreateWebhook(string eventId) =>
        BillingWebhookEvent.CreateProcessed(BillingProviderNames.Stripe, eventId, "invoice.paid", "invoice", DateTime.UtcNow, "{}");

    private static BillingPayment CreatePayment(UserId userId, string paymentId, Guid? subscriptionId = null) =>
        BillingPayment.Create(userId, subscriptionId, BillingProviderNames.Stripe, paymentId,
            externalCustomerId: null, externalSubscriptionId: null, externalPaymentMethodId: null,
            externalPriceId: null, plan: null, status: "succeeded", kind: "subscription",
            amount: 10m, currency: "USD", currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: null, providerMetadataJson: null);

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddBillingModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
