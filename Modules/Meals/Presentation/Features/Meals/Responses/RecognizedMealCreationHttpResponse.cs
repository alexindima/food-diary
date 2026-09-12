namespace FoodDiary.Presentation.Api.Features.Meals.Responses;

public sealed record RecognizedMealCreationHttpResponse(Guid OperationId, Guid MealId, DateTime UndoUntilUtc, bool Undone);
