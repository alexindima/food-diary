using FoodDiary.Application.Hydration.Commands.CreateHydrationFromOperation;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationOperationHttpMappings {
    extension(CreateHydrationFromOperationHttpRequest request) {
        public CreateHydrationFromOperationCommand ToCommand(Guid userId, Guid operationId) =>
            new(userId, operationId, request.TimestampUtc, request.AmountMl);
    }

    extension(HydrationOperationModel model) {
        public HydrationOperationHttpResponse ToHttpResponse() => new(model.OperationId, model.EntryId, model.TimestampUtc, model.AmountMl);
    }
}
