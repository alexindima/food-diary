using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpMappings {
    public static DeleteHydrationEntryCommand ToDeleteCommand(this Guid id, Guid userId) =>
        new(userId, id);

    public static CreateHydrationEntryCommand ToCommand(this CreateHydrationEntryHttpRequest request, Guid userId) =>
        new(
            userId,
            request.TimestampUtc,
            request.AmountMl);

    public static UpdateHydrationEntryCommand ToCommand(
        this UpdateHydrationEntryHttpRequest request,
        Guid userId,
        Guid entryId) =>
        new(
            userId,
            entryId,
            request.TimestampUtc,
            request.AmountMl);
}
