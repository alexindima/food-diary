using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpMappings {
    public static CreateHydrationEntryCommand ToCommand(this CreateHydrationEntryHttpRequest request, Guid userId) =>
        new(
            new UserId(userId),
            request.TimestampUtc,
            request.AmountMl);

    public static UpdateHydrationEntryCommand ToCommand(
        this UpdateHydrationEntryHttpRequest request,
        Guid userId,
        Guid entryId) =>
        new(
            new UserId(userId),
            new HydrationEntryId(entryId),
            request.TimestampUtc,
            request.AmountMl);
}
