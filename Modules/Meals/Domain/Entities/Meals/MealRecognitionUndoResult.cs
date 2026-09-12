namespace FoodDiary.Domain.Entities.Meals;

public enum MealRecognitionUndoResult {
    Undone = 0,
    AlreadyUndone = 1,
    AlreadyDeleted = 2,
    Expired = 3,
    Changed = 4,
}
