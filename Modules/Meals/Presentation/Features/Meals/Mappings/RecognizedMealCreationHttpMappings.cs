using FoodDiary.Application.Meals.Commands.CreateMealFromRecognition;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Presentation.Api.Features.Meals.Requests;
using FoodDiary.Presentation.Api.Features.Meals.Responses;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class RecognizedMealCreationHttpMappings {
    extension(CreateMealFromRecognitionHttpRequest request) {
        public CreateMealFromRecognitionCommand ToCommand(Guid userId, Guid recognitionId) => new(userId, recognitionId, request.OccurredAtUtc);
    }

    extension(RecognizedMealCreationModel model) {
        public RecognizedMealCreationHttpResponse ToHttpResponse() => new(model.OperationId, model.MealId, model.UndoUntilUtc, model.Undone);
    }
}
