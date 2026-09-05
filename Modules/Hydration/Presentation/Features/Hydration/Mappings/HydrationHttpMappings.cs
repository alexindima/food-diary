using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpMappings {
    extension(Guid id) {
        public DeleteHydrationEntryCommand ToDeleteCommand(Guid userId) =>
                new(userId, id);
    }

    extension(CreateHydrationEntryHttpRequest request) {
        public CreateHydrationEntryCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.TimestampUtc,
                    request.AmountMl);
    }

    extension(UpdateHydrationEntryHttpRequest request) {
        public UpdateHydrationEntryCommand ToCommand(
                Guid userId,
                Guid entryId) =>
                new(
                    userId,
                    entryId,
                    request.TimestampUtc,
                    request.AmountMl);
    }
}
