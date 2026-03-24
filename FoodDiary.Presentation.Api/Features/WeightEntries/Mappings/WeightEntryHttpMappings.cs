using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpMappings {
    public static DeleteWeightEntryCommand ToDeleteCommand(this Guid id, Guid userId) =>
        new(userId, new WeightEntryId(id));

    public static CreateWeightEntryCommand ToCommand(this CreateWeightEntryHttpRequest request, Guid userId) =>
        new(
            userId,
            request.Date,
            request.Weight);

    public static UpdateWeightEntryCommand ToCommand(
        this UpdateWeightEntryHttpRequest request,
        Guid userId,
        Guid entryId) =>
        new(
            userId,
            new WeightEntryId(entryId),
            request.Date,
            request.Weight);
}
