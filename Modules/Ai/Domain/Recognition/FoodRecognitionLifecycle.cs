namespace FoodDiary.Modules.Ai.Domain.Recognition;

/// <summary>Pure transition rules. Persistence must atomically check the source state.</summary>
public static class FoodRecognitionLifecycle {
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";

    public static FoodRecognitionStatus Decode(string? value) => value switch {
        Queued => FoodRecognitionStatus.Queued,
        Running => FoodRecognitionStatus.Running,
        Succeeded => FoodRecognitionStatus.Succeeded,
        Failed => FoodRecognitionStatus.Failed,
        _ => FoodRecognitionStatus.Unknown,
    };

    public static string Encode(FoodRecognitionStatus status) => status switch {
        FoodRecognitionStatus.Queued => Queued,
        FoodRecognitionStatus.Running => Running,
        FoodRecognitionStatus.Succeeded => Succeeded,
        FoodRecognitionStatus.Failed => Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static FoodRecognitionStatus? Next(FoodRecognitionStatus current, FoodRecognitionAction action) => (current, action) switch {
        (FoodRecognitionStatus.Queued, FoodRecognitionAction.Claim) => FoodRecognitionStatus.Running,
        (FoodRecognitionStatus.Running, FoodRecognitionAction.CheckpointVision) => FoodRecognitionStatus.Running,
        (FoodRecognitionStatus.Running, FoodRecognitionAction.Complete) => FoodRecognitionStatus.Succeeded,
        (FoodRecognitionStatus.Running, FoodRecognitionAction.Fail) => FoodRecognitionStatus.Failed,
        (FoodRecognitionStatus.Queued or FoodRecognitionStatus.Running, FoodRecognitionAction.Interrupt) => FoodRecognitionStatus.Failed,
        _ => null,
    };

    public static bool IsPending(FoodRecognitionStatus status) => status is FoodRecognitionStatus.Queued or FoodRecognitionStatus.Running;
}
