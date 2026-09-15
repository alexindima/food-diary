using FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationEntry;
using FoodDiary.Modules.Hydration.Application.Commands.DeleteHydrationEntry;
using FoodDiary.Modules.Hydration.Application.Commands.UpdateHydrationEntry;
using FoodDiary.Modules.Hydration.Presentation.Requests;

namespace FoodDiary.Modules.Hydration.Presentation.Mappings;

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
