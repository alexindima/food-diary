using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;

namespace FoodDiary.Modules.Wearables.Application.Commands.ConnectWearable;

public record ConnectWearableCommand(
    Guid? UserId,
    string Provider,
    string Code,
    string State,
    string RequestId,
    string RequestHash) : ICommand<Result<WearableConnectionModel>>, IUserRequest;
