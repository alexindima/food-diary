using FoodDiary.Modules.Meals.Application.Commands.UndoRecognizedMeal;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Modules.Meals.Presentation.Responses;

namespace FoodDiary.Modules.Meals.Presentation.Mappings;

public static class RecognizedMealUndoHttpMappings {
    extension(Guid operationId) {
        public UndoRecognizedMealCommand ToUndoRecognizedMealCommand(Guid userId) => new(userId, operationId);
    }

    extension(RecognizedMealUndoModel model) {
        public RecognizedMealUndoHttpResponse ToHttpResponse() => new(model.Status);
    }
}
