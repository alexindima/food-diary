using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using Microsoft.Extensions.Logging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.SendCancellationConfirmations;

public sealed class SendCancellationConfirmationsCommandHandler(
    IBillingCancellationRepository cancellations,
    IBillingTransactionRunner transactions,
    ISender sender,
    TimeProvider timeProvider,
    ILogger<SendCancellationConfirmationsCommandHandler> logger)
    : IRequestHandler<SendCancellationConfirmationsCommand, BillingCancellationEmailRunResult> {
    public async Task<BillingCancellationEmailRunResult> Handle(SendCancellationConfirmationsCommand request, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BatchSize, nameof(request));
        IReadOnlyList<BillingCancellationEmailCandidateModel> pending = await cancellations.GetPendingConfirmationsAsync(request.BatchSize, cancellationToken);
        int queued = 0;
        int skipped = 0;
        int failed = 0;
        foreach (BillingCancellationEmailCandidateModel candidate in pending) {
            cancellationToken.ThrowIfCancellationRequested();
            try {
                bool sent = await QueueConfirmationAsync(candidate, cancellationToken);
                if (sent) {
                    queued++;
                } else {
                    skipped++;
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception exception) {
                failed++;
                logger.LogWarning(exception, "Could not queue cancellation confirmation {CancellationId}; the intent remains retryable.", candidate.Id);
            }
        }
        return new BillingCancellationEmailRunResult(queued, skipped, failed);
    }

    private async Task<bool> QueueConfirmationAsync(BillingCancellationEmailCandidateModel candidate, CancellationToken cancellationToken) {
        bool queued = false;
        await transactions.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(candidate.UserId.Value), async token => {
            queued = false;
            BillingCancellation? cancellation = await cancellations.GetOwnedAsync(candidate.Id, candidate.UserId, token);
            if (cancellation is null || cancellation.ConfirmationEmailHandledAtUtc is not null) {
                return;
            }
            UserBillingProfileModel? user = await sender.Send(new GetUserBillingProfileIncludingDeletedQuery(candidate.UserId), token);
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            if (cancellation.Outcome is "scheduled" or "canceled" &&
                user is { IsActive: true, IsDeleted: false, IsEmailConfirmed: true } && !string.IsNullOrWhiteSpace(user.Email)) {
                DateTime? accessUntil = cancellation.EffectiveAtUtc > now ? cancellation.EffectiveAtUtc : null;
                Result result = await sender.Send(new QueueSubscriptionCancellationEmailCommand(user.Email, user.Language,
                    user.TimeZoneId, accessUntil, $"billing-cancellation:{cancellation.Id:N}"), token);
                if (result.IsFailure) {
                    throw new InvalidOperationException("Cancellation confirmation was not queued.");
                }
                queued = true;
            }
            cancellation.MarkConfirmationEmailHandled(now);
            await cancellations.UpdateAsync(cancellation, token);
        }, cancellationToken);
        return queued;
    }
}
