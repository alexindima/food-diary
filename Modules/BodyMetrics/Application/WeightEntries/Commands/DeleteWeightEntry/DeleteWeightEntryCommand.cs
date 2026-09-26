using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.DeleteWeightEntry;

public record DeleteWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId
) : ICommand<Result>, IUserRequest;
