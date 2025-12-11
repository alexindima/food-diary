using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Mappings;

public static class HydrationRequestMappings
{
    public static CreateHydrationEntryCommand ToCommand(this CreateHydrationEntryRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.TimestampUtc,
            request.AmountMl);

    public static UpdateHydrationEntryCommand ToCommand(this UpdateHydrationEntryRequest request, Guid? userId, Guid entryId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new HydrationEntryId(entryId),
            request.TimestampUtc,
            request.AmountMl);
}
