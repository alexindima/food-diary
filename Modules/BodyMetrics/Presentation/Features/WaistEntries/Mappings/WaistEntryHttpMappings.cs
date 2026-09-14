using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Mappings;

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
                    request.CircumferenceCm);
    }

    extension(UpdateWaistEntryHttpRequest request) {
        public UpdateWaistEntryCommand ToCommand(
                Guid userId,
                Guid entryId) =>
                new(
                    userId,
                    entryId,
                    request.Date,
                    request.CircumferenceCm);
    }
}
