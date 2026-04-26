using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Wearables.Models;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public record ConnectWearableCommand(
    Guid? UserId,
    string Provider,
    string Code) : ICommand<Result<WearableConnectionModel>>, IUserRequest;
