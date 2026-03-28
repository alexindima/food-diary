using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public record DeleteWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId
) : ICommand<Result<bool>>, IUserRequest;
