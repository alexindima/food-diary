using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public record DeleteWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId
) : ICommand<Result>, IUserRequest;
