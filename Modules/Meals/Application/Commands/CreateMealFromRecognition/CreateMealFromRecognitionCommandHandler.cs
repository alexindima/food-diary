using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Meals.Commands.CreateMealFromRecognition;

public sealed class CreateMealFromRecognitionCommandHandler(
    IMealRecognitionTransactionRunner transactions,
    IMealRecognitionReceiptRepository receipts,
    IFoodRecognitionResultReader recognitionResults,
    FoodDiary.Mediator.IRequestHandler<CreateMealCommand, Result<MealModel>> createMeal,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider) : ICommandHandler<CreateMealFromRecognitionCommand, Result<RecognizedMealCreationModel>> {
    public async Task<Result<RecognizedMealCreationModel>> Handle(CreateMealFromRecognitionCommand command, CancellationToken cancellationToken) {
        Result<UserId> owner = await CurrentUserAccessResolver.ResolveAsync(command.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure) {
            return Result.Failure<RecognizedMealCreationModel>(owner.Error);
        }
        if (command.RecognitionId == Guid.Empty || command.OccurredAtUtc.Kind != DateTimeKind.Utc) {
            return Failure("Meal.InvalidRecognitionOperation", "Recognition ID and UTC meal time are required.", ErrorKind.Validation);
        }
        // PostgreSQL timestamps have microsecond precision; canonicalize before comparing replay payloads.
        var occurredAt = new DateTime(command.OccurredAtUtc.Ticks / 10 * 10, DateTimeKind.Utc);
        return await transactions.ExecuteSerializedAsync(owner.Value, async token => {
            Result<UserId> access = await CurrentUserAccessResolver.ResolveAsync(owner.Value, currentUserAccessService, token).ConfigureAwait(false);
            if (access.IsFailure) {
                return Result.Failure<RecognizedMealCreationModel>(access.Error);
            }
            // Recognition ID is also the permanent operation ID; clients cannot remap a job to another operation.
            MealRecognitionReceipt? existing = await receipts.FindByRecognitionAsync(owner.Value, command.RecognitionId, token).ConfigureAwait(false);
            if (existing is not null) {
                return await ReplayAsync(existing, owner.Value, command.RecognitionId, occurredAt, token).ConfigureAwait(false);
            }
            Result<FoodRecognitionJobModel> recognized = await recognitionResults.GetCompletedAsync(owner.Value.Value, command.RecognitionId, token).ConfigureAwait(false);
            if (recognized.IsFailure) {
                return Result.Failure<RecognizedMealCreationModel>(recognized.Error);
            }
            FoodRecognitionJobModel job = recognized.Value;
            if (job.UserId != owner.Value.Value || job.Id != command.RecognitionId || job.ImageAssetId == Guid.Empty || job.Nutrition?.Items is not { Count: > 0 }) {
                return Failure("Meal.InvalidRecognitionResult", "Recognition does not contain an owned complete result.", ErrorKind.Validation);
            }
            CreateMealCommand create = ToCreateCommand(owner.Value, occurredAt, job);
            ValidationResult validation = await new CreateMealCommandValidator().ValidateAsync(create, token).ConfigureAwait(false);
            if (!validation.IsValid) {
                return Failure("Meal.InvalidRecognitionResult", "Recognition contains invalid meal values.", ErrorKind.Validation);
            }
            Result<MealModel> created = await createMeal.Handle(create, token).ConfigureAwait(false);
            if (created.IsFailure) {
                return Result.Failure<RecognizedMealCreationModel>(created.Error);
            }
            var mealId = new MealId(created.Value.Id);
            uint version = await transactions.FlushCreatedMealAsync(mealId, owner.Value, token).ConfigureAwait(false);
            var receipt = MealRecognitionReceipt.Create(command.RecognitionId, owner.Value, command.RecognitionId,
                mealId, version, occurredAt, timeProvider.GetUtcNow().UtcDateTime, TimeSpan.FromHours(24));
            await receipts.AddAsync(receipt, token).ConfigureAwait(false);
            return Result.Success(ToModel(receipt));
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<RecognizedMealCreationModel>> ReplayAsync(MealRecognitionReceipt receipt, UserId owner,
        Guid recognitionId, DateTime occurredAt, CancellationToken cancellationToken) {
        if (!receipt.Matches(owner, recognitionId, occurredAt)) {
            return Failure("Meal.RecognitionConflict", "The recognition was already saved with different input.", ErrorKind.Conflict);
        }
        if (!receipt.UndoneAtUtc.HasValue) {
            // Reuse the owned row lock inside the recognition transaction so a manual deletion
            // cannot be reported as a fresh save. Never recreate or undo a still-present meal.
            (Meal Meal, uint Version)? current = await receipts.LockMealForUndoAsync(owner, receipt.MealId, cancellationToken).ConfigureAwait(false);
            if (current is null) {
                receipt.TryUndo(currentMealVersion: null, timeProvider.GetUtcNow().UtcDateTime);
            }
        }
        return Result.Success(ToModel(receipt));
    }

    private static CreateMealCommand ToCreateCommand(UserId owner, DateTime occurredAt, FoodRecognitionJobModel job) {
        // Nutrition owns the quantities and nutrients. Preserve those pairs even if vision order differs.
        MealAiItemInput[] items = [.. job.Nutrition!.Items.Select(item => new MealAiItemInput(
            item.Name, NameLocal: null, (double)item.Amount, item.Unit, (double)item.Calories, (double)item.Protein,
            (double)item.Fat, (double)item.Carbs, (double)item.Fiber, (double)item.Alcohol))];
        return new CreateMealCommand(owner.Value, occurredAt, MealType: null, Comment: job.Description,
            ImageUrl: null, job.ImageAssetId, Items: [],
            AiSessions: [new MealAiSessionInput(job.ImageAssetId, "Photo", job.UpdatedOnUtc, Notes: null, items)],
            IsNutritionAutoCalculated: true, ManualCalories: null, ManualProteins: null, ManualFats: null,
            ManualCarbs: null, ManualFiber: null, ManualAlcohol: null, PreMealSatietyLevel: 0, PostMealSatietyLevel: 0);
    }

    private static RecognizedMealCreationModel ToModel(MealRecognitionReceipt receipt) =>
        new(receipt.OperationId, receipt.MealId.Value, receipt.UndoUntilUtc, receipt.UndoneAtUtc.HasValue);

    private static Result<RecognizedMealCreationModel> Failure(string code, string message, ErrorKind kind) =>
        Result.Failure<RecognizedMealCreationModel>(new Error(code, message, kind));
}
