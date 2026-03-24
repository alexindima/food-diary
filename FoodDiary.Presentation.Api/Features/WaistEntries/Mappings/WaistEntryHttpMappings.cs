using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpMappings {
    public static DeleteWaistEntryCommand ToDeleteCommand(this Guid id, Guid userId) =>
        new(userId, id);

    public static CreateWaistEntryCommand ToCommand(this CreateWaistEntryHttpRequest request, Guid userId) =>
        new(
            userId,
            request.Date,
            request.Circumference);

    public static UpdateWaistEntryCommand ToCommand(
        this UpdateWaistEntryHttpRequest request,
        Guid userId,
        Guid entryId) =>
        new(
            userId,
            entryId,
            request.Date,
            request.Circumference);
}
