namespace FoodDiary.Modules.Meals.Presentation.Responses;

public sealed record RecognizedMealCreationHttpResponse(Guid OperationId, Guid MealId, DateTime UndoUntilUtc, bool Undone);
