using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpMappings {
    public static CreateWaistEntryCommand ToCommand(this CreateWaistEntryHttpRequest request, Guid userId) =>
        new(
            new UserId(userId),
            request.Date,
            request.Circumference);

    public static UpdateWaistEntryCommand ToCommand(
        this UpdateWaistEntryHttpRequest request,
        Guid userId,
        Guid entryId) =>
        new(
            new UserId(userId),
            new WaistEntryId(entryId),
            request.Date,
            request.Circumference);
}
