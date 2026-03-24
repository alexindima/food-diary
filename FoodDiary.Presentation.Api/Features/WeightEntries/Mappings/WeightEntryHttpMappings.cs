using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpMappings {
    public static DeleteWeightEntryCommand ToDeleteCommand(this Guid id, Guid userId) =>
        new(userId, id);

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
            entryId,
            request.Date,
            request.Weight);
}
