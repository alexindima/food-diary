using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpMappings {
    extension(Guid id) {
        public DeleteWaistEntryCommand ToDeleteCommand(Guid userId) =>
                new(userId, id);
    }

    extension(CreateWaistEntryHttpRequest request) {
        public CreateWaistEntryCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.Date,
                    request.Circumference);
    }

    extension(UpdateWaistEntryHttpRequest request) {
        public UpdateWaistEntryCommand ToCommand(
                Guid userId,
                Guid entryId) =>
                new(
                    userId,
                    entryId,
                    request.Date,
                    request.Circumference);
    }
}
