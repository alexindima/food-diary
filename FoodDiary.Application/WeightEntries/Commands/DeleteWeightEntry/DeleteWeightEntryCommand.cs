using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public record DeleteWeightEntryCommand(
    Guid? UserId,
    WeightEntryId WeightEntryId
) : ICommand<Result<bool>>;
