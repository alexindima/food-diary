using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerFact]
    public async Task CancellationConfirmation_RollsBackQueueAndRetriesExactlyOnceAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await VerifyCancellationEmailAtomicityAsync(context);
    }

    public static async Task VerifyCancellationEmailAtomicityAsync(FoodDiaryDbContext context) {
        var user = User.Create("confirmation-" + Guid.NewGuid().ToString("N") + "@example.test", "hash");
        DateTime now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.Paddle, "customer", "price", "monthly");
        subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_atomic_email", externalPaymentMethodId: null,
            "price", "monthly", "active", now, now.AddDays(30), cancelAtPeriodEnd: true, canceledAtUtc: null,
            trialStartUtc: null, trialEndUtc: null, "evt_atomic_email", now, webhookOccurredAtUtc: now);
        var cancellation = BillingCancellation.Create(subscription, "fooddiary", now, now.AddDays(30));
        context.Users.Add(user);
        context.BillingCancellations.Add(cancellation);
        await context.SaveChangesAsync();
        string correlation = $"billing-cancellation:{cancellation.Id:N}";
        await using ServiceProvider provider = CreateProvider(context);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>())
            .Returns(new UserBillingProfileModel(user.Id, user.Email, IsActive: true, IsDeleted: false, HasPaidPremium: true,
                PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true));
        bool failAfterSave = true;
        sender.Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>()).Returns(async call => {
            QueueSubscriptionCancellationEmailCommand command = call.Arg<QueueSubscriptionCancellationEmailCommand>();
            await provider.GetRequiredService<IEmailOutbox>().EnqueueAsync(new EmailMessage("noreply@example.test", "FoodDiary",
                [command.ToEmail], "Synthetic confirmation", "<p>Synthetic confirmation</p>", "Synthetic confirmation",
                Purpose: "subscription_cancellation", CorrelationId: command.CorrelationId), call.Arg<CancellationToken>());
            // Reproduce a crash after the shared outbox has actually written, before the handled marker commits.
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(call.Arg<CancellationToken>());
            if (failAfterSave) {
                throw new InvalidOperationException("Injected failure after outbox save.");
            }
            return Result.Success();
        });
        var handler = new SendCancellationConfirmationsCommandHandler(provider.GetRequiredService<IBillingCancellationRepository>(),
            provider.GetRequiredService<IBillingTransactionRunner>(), sender, TimeProvider.System,
            NullLogger<SendCancellationConfirmationsCommandHandler>.Instance);
        Assert.Equal(new BillingCancellationEmailRunResult(0, 0, 1),
            await handler.Handle(new SendCancellationConfirmationsCommand(100), CancellationToken.None));
        Assert.False(await context.EmailOutbox.AsNoTracking().AnyAsync(item => item.CorrelationId == correlation));
        Assert.Null((await context.BillingCancellations.AsNoTracking().SingleAsync(item => item.Id == cancellation.Id)).ConfirmationEmailHandledAtUtc);
        failAfterSave = false;
        Assert.Equal(1, (await handler.Handle(new SendCancellationConfirmationsCommand(100), CancellationToken.None)).Queued);
        Assert.Equal(0, (await handler.Handle(new SendCancellationConfirmationsCommand(100), CancellationToken.None)).Queued);
        Assert.Single(await context.EmailOutbox.AsNoTracking().Where(item => item.CorrelationId == correlation).ToListAsync());
        Assert.NotNull((await context.BillingCancellations.AsNoTracking().SingleAsync(item => item.Id == cancellation.Id)).ConfirmationEmailHandledAtUtc);
    }
}
