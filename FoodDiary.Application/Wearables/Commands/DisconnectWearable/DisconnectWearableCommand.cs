using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Wearables.Commands.DisconnectWearable;

public record DisconnectWearableCommand(
    Guid? UserId,
    string Provider) : ICommand<Result>, IUserRequest;
