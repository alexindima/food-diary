namespace FoodDiary.Modules.Meals.Application.Models;

public sealed record RecognizedMealCreationModel(Guid OperationId, Guid MealId, DateTime UndoUntilUtc, bool Undone);
