using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public record ConnectWearableCommand(
    Guid? UserId,
    string Provider,
    string Code,
    string State) : ICommand<Result<WearableConnectionModel>>, IUserRequest;
