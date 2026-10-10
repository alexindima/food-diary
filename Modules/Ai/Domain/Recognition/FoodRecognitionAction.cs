namespace FoodDiary.Modules.Ai.Domain.Recognition;

public enum FoodRecognitionAction {
    None = 0,
    Claim = 1,
    CheckpointVision = 2,
    Complete = 3,
    Fail = 4,
    Interrupt = 5,
}
