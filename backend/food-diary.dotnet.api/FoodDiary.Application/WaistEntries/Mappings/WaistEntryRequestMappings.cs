using System;
using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Mappings;

public static class WaistEntryRequestMappings
{
    public static CreateWaistEntryCommand ToCommand(this CreateWaistEntryRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.Date,
            request.Circumference);

    public static UpdateWaistEntryCommand ToCommand(this UpdateWaistEntryRequest request, Guid? userId, Guid entryId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new WaistEntryId(entryId),
            request.Date,
            request.Circumference);
}
