using FoodDiary.Modules.Meals.Application.Commands.CreateMealFromRecognition;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Meals.Presentation.Responses;

namespace FoodDiary.Modules.Meals.Presentation.Mappings;

public static class RecognizedMealCreationHttpMappings {
    extension(CreateMealFromRecognitionHttpRequest request) {
        public CreateMealFromRecognitionCommand ToCommand(Guid userId, Guid recognitionId) => new(userId, recognitionId, request.OccurredAtUtc);
    }

    extension(RecognizedMealCreationModel model) {
        public RecognizedMealCreationHttpResponse ToHttpResponse() => new(model.OperationId, model.MealId, model.UndoUntilUtc, model.Undone);
    }
}
