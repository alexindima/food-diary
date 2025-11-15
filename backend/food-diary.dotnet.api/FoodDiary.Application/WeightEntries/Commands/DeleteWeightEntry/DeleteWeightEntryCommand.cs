using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public record DeleteWeightEntryCommand(
    UserId? UserId,
    WeightEntryId WeightEntryId
) : ICommand<Result<bool>>;
