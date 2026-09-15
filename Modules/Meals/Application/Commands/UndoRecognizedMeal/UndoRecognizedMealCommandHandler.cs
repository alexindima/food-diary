using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Meals.Application.Commands.UndoRecognizedMeal;

public sealed class UndoRecognizedMealCommandHandler(
    IMealRecognitionTransactionRunner transactions,
    IMealRecognitionReceiptRepository receipts,
    IMealWriteRepository meals,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider) : ICommandHandler<UndoRecognizedMealCommand, Result<RecognizedMealUndoModel>> {
    public async Task<Result<RecognizedMealUndoModel>> Handle(UndoRecognizedMealCommand command, CancellationToken cancellationToken) {
        Result<UserId> owner = await CurrentUserAccessResolver.ResolveAsync(command.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure) {
            return Result.Failure<RecognizedMealUndoModel>(owner.Error);
        }
        if (command.OperationId == Guid.Empty) {
            return Result.Failure<RecognizedMealUndoModel>(new Error("Meal.InvalidRecognitionOperation", "An operation ID is required.", ErrorKind.Validation));
        }
        return await transactions.ExecuteSerializedAsync(owner.Value, async token => {
            // Recheck after acquiring the transaction lock and on every retried attempt.
            Result<UserId> access = await CurrentUserAccessResolver.ResolveAsync(owner.Value, currentUserAccessService, token).ConfigureAwait(false);
            if (access.IsFailure) {
                return Result.Failure<RecognizedMealUndoModel>(access.Error);
            }
            MealRecognitionReceipt? receipt = await receipts.FindAsync(owner.Value, command.OperationId, token).ConfigureAwait(false);
            if (receipt is null || receipt.UserId != owner.Value || receipt.OperationId != command.OperationId) {
                return NotFound();
            }
            if (receipt.UndoneAtUtc.HasValue) {
                return Result.Success(new RecognizedMealUndoModel("AlreadyUndone"));
            }
            (Meal Meal, uint Version)? locked = await receipts.LockMealForUndoAsync(owner.Value, receipt.MealId, token).ConfigureAwait(false);
            if (locked.HasValue && (locked.Value.Meal.UserId != owner.Value || locked.Value.Meal.Id != receipt.MealId)) {
                return NotFound();
            }
            MealRecognitionUndoResult outcome = receipt.TryUndo(locked?.Version, timeProvider.GetUtcNow().UtcDateTime);
            if (outcome == MealRecognitionUndoResult.Changed) {
                return Result.Failure<RecognizedMealUndoModel>(new Error("Meal.RecognitionUndoChanged", "The meal has been edited and cannot be undone.", ErrorKind.Conflict));
            }
            if (outcome == MealRecognitionUndoResult.Expired) {
                return Result.Failure<RecognizedMealUndoModel>(new Error("Meal.RecognitionUndoExpired", "The undo period has expired.", ErrorKind.Conflict));
            }
            if (outcome == MealRecognitionUndoResult.Undone) {
                await meals.DeleteAsync(locked!.Value.Meal, token).ConfigureAwait(false);
            }
            return Result.Success(new RecognizedMealUndoModel(outcome.ToString()));
        }, cancellationToken).ConfigureAwait(false);
    }

    private static Result<RecognizedMealUndoModel> NotFound() => Result.Failure<RecognizedMealUndoModel>(
        new Error("Meal.RecognitionOperationNotFound", "The recognition operation was not found.", ErrorKind.NotFound));
}
