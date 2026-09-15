using FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationFromOperation;
using FoodDiary.Modules.Hydration.Application.Models;
using FoodDiary.Modules.Hydration.Presentation.Requests;
using FoodDiary.Modules.Hydration.Presentation.Responses;

namespace FoodDiary.Modules.Hydration.Presentation.Mappings;

public static class HydrationOperationHttpMappings {
    extension(CreateHydrationFromOperationHttpRequest request) {
        public CreateHydrationFromOperationCommand ToCommand(Guid userId, Guid operationId) =>
            new(userId, operationId, request.TimestampUtc, request.AmountMl);
    }

    extension(HydrationOperationModel model) {
        public HydrationOperationHttpResponse ToHttpResponse() => new(model.OperationId, model.EntryId, model.TimestampUtc, model.AmountMl);
    }
}
