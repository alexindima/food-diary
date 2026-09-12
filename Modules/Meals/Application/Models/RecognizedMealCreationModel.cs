namespace FoodDiary.Application.Meals.Models;

public sealed record RecognizedMealCreationModel(Guid OperationId, Guid MealId, DateTime UndoUntilUtc, bool Undone);
