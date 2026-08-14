using FoodDiary.Application.BodyMetrics.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.BodyMetrics.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.BodyMetrics.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpMappings {
    extension(Guid id) {
        public DeleteWeightEntryCommand ToDeleteCommand(Guid userId) =>
                new(userId, id);
    }

    extension(CreateWeightEntryHttpRequest request) {
        public CreateWeightEntryCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.Date,
                    request.Weight);
    }

    extension(UpdateWeightEntryHttpRequest request) {
        public UpdateWeightEntryCommand ToCommand(
                Guid userId,
                Guid entryId) =>
                new(
                    userId,
                    entryId,
                    request.Date,
                    request.Weight);
    }
}
