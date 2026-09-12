using FoodDiary.Application.Meals.Commands.UndoRecognizedMeal;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Presentation.Api.Features.Meals.Responses;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class RecognizedMealUndoHttpMappings {
    extension(Guid operationId) {
        public UndoRecognizedMealCommand ToUndoRecognizedMealCommand(Guid userId) => new(userId, operationId);
    }

    extension(RecognizedMealUndoModel model) {
        public RecognizedMealUndoHttpResponse ToHttpResponse() => new(model.Status);
    }
}
