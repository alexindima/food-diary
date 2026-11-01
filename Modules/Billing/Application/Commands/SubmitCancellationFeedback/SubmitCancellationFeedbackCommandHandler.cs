using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;

public sealed class SubmitCancellationFeedbackCommandHandler(ISender sender, IBillingCancellationRepository cancellations,
    TimeProvider timeProvider, IBillingTransactionRunner transactionRunner)
    : IRequestHandler<SubmitCancellationFeedbackCommand, Result> {
    public async Task<Result> Handle(SubmitCancellationFeedbackCommand request, CancellationToken cancellationToken) {
        Result<UserId> owner = await BillingCurrentUserAccessResolver.ResolveAsync(request.UserId, sender, cancellationToken);
        if (owner.IsFailure) {
            return Result.Failure(owner.Error);
        }
        var result = Result.Success();
        await transactionRunner.ExecuteSerializedAsync(BillingOperationLockKeys.ForUser(owner.Value.Value), async token => {
            result = Result.Success();
            BillingCancellation? cancellation = await cancellations.GetOwnedAsync(request.CancellationId, owner.Value, token);
            if (cancellation is null) {
                result = Result.Failure(BillingErrors.CancellationNotFound);
                return;
            }
            try {
                cancellation.SubmitFeedback(request.ReasonCode, request.Comment, timeProvider.GetUtcNow().UtcDateTime);
            } catch (ArgumentException) {
                result = Result.Failure(BillingErrors.InvalidCancellationFeedback);
                return;
            }
            await cancellations.UpdateAsync(cancellation, token);
        }, cancellationToken);
        return result;
    }
}
