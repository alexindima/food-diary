using System;
using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Mappings;

public static class WeightEntryRequestMappings
{
    public static CreateWeightEntryCommand ToCommand(this CreateWeightEntryRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.Date,
            request.Weight);

    public static UpdateWeightEntryCommand ToCommand(this UpdateWeightEntryRequest request, Guid? userId, Guid entryId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new WeightEntryId(entryId),
            request.Date,
            request.Weight);
}
