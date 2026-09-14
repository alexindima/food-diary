using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Requests;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Mappings;

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
                    request.WeightKg);
    }

    extension(UpdateWeightEntryHttpRequest request) {
        public UpdateWeightEntryCommand ToCommand(
                Guid userId,
                Guid entryId) =>
                new(
                    userId,
                    entryId,
                    request.Date,
                    request.WeightKg);
    }
}
