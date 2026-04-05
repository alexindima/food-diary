using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Wearables.Models;

namespace FoodDiary.Application.Wearables.Commands.ConnectWearable;

public record ConnectWearableCommand(
    Guid? UserId,
    string Provider,
    string Code) : ICommand<Result<WearableConnectionModel>>, IUserRequest;
