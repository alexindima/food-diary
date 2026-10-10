namespace FoodDiary.Modules.Ai.Domain.Recognition;

public enum FoodRecognitionStatus {
    Unknown = 0,
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
}
