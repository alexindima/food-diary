using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Meals;

public sealed class MealRecognitionReceipt {
    public Guid OperationId { get; private init; }
    public UserId UserId { get; private init; }
    public Guid RecognitionId { get; private init; }
    public MealId MealId { get; private init; }
    public uint MealVersion { get; private init; }
    public DateTime MealOccurredAtUtc { get; private init; }
    public DateTime SavedAtUtc { get; private init; }
    public DateTime UndoUntilUtc { get; private init; }
    public DateTime? UndoneAtUtc { get; private set; }

    private MealRecognitionReceipt() { }

    public static MealRecognitionReceipt Create(Guid operationId, UserId userId, Guid recognitionId, MealId mealId,
        uint mealVersion, DateTime occurredAtUtc, DateTime savedAtUtc, TimeSpan undoWindow) {
        if (operationId == Guid.Empty || userId.Value == Guid.Empty || recognitionId == Guid.Empty || mealId.Value == Guid.Empty || mealVersion == 0) {
            throw new ArgumentException("A recognition receipt requires valid operation, owner, recognition and meal identities.", nameof(operationId));
        }
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureUtc(savedAtUtc, nameof(savedAtUtc));
        if (undoWindow <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(undoWindow));
        }
        return new MealRecognitionReceipt {
            OperationId = operationId,
            UserId = userId,
            RecognitionId = recognitionId,
            MealId = mealId,
            MealVersion = mealVersion,
            MealOccurredAtUtc = occurredAtUtc,
            SavedAtUtc = savedAtUtc,
            UndoUntilUtc = savedAtUtc.Add(undoWindow),
        };
    }

    public bool Matches(UserId userId, Guid recognitionId, DateTime occurredAtUtc) =>
        UserId == userId && RecognitionId == recognitionId && MealOccurredAtUtc == occurredAtUtc;

    public MealRecognitionUndoResult TryUndo(uint? currentMealVersion, DateTime nowUtc) {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (UndoneAtUtc.HasValue) {
            return MealRecognitionUndoResult.AlreadyUndone;
        }
        if (!currentMealVersion.HasValue) {
            UndoneAtUtc = nowUtc;
            return MealRecognitionUndoResult.AlreadyDeleted;
        }
        if (nowUtc >= UndoUntilUtc) {
            return MealRecognitionUndoResult.Expired;
        }
        if (currentMealVersion.Value != MealVersion) {
            return MealRecognitionUndoResult.Changed;
        }
        UndoneAtUtc = nowUtc;
        return MealRecognitionUndoResult.Undone;
    }

    private static void EnsureUtc(DateTime value, string parameterName) {
        if (value.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A UTC timestamp is required.", parameterName);
        }
    }
}
