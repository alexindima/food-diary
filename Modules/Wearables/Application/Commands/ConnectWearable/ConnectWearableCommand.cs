using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public record ConnectWearableCommand(
    Guid? UserId,
    string Provider,
    string Code,
    string State,
    string RequestId,
    string RequestHash) : ICommand<Result<WearableConnectionModel>>, IUserRequest;
