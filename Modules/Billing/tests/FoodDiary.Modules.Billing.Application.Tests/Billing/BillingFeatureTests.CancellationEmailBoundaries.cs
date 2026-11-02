using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task CancellationEmail_QueueResultFailureLeavesTheIntentRetryable() {
        var fixture = new CancellationEmailFixture();
        fixture.Sender.Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Email.Unavailable", "Unavailable.")));

        Assert.Equal(new BillingCancellationEmailRunResult(0, 0, 1), await fixture.RunAsync());
        Assert.Null(fixture.Rows[0].ConfirmationEmailHandledAtUtc);
    }

    [Fact]
    public async Task CancellationEmail_CallerCancellationIsPropagatedWithoutCountingAFailedDelivery() {
        using var cancellation = new CancellationTokenSource();
        IBillingCancellationRepository repository = Substitute.For<IBillingCancellationRepository>();
        var candidate = new BillingCancellationEmailCandidateModel(BillingCancellationId.New(), UserId.New());
        repository.GetPendingConfirmationsAsync(100, cancellation.Token).Returns([candidate]);
        repository.GetOwnedAsync(candidate.Id, candidate.UserId, cancellation.Token).Returns<Task<BillingCancellation?>>(async _ => {
            await cancellation.CancelAsync();
            throw new OperationCanceledException(cancellation.Token);
        });
        var handler = new SendCancellationConfirmationsCommandHandler(repository, new NoOpBillingTransactionRunner(),
            Substitute.For<ISender>(), TimeProvider.System, NullLogger<SendCancellationConfirmationsCommandHandler>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.Handle(new SendCancellationConfirmationsCommand(100), cancellation.Token));
        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CancellationEmail_InvalidBatchDoesNotReadPendingIntents(int batch) {
        IBillingCancellationRepository repository = Substitute.For<IBillingCancellationRepository>();
        var handler = new SendCancellationConfirmationsCommandHandler(repository, new NoOpBillingTransactionRunner(),
            Substitute.For<ISender>(), TimeProvider.System, NullLogger<SendCancellationConfirmationsCommandHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.Handle(new SendCancellationConfirmationsCommand(batch), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }
}
