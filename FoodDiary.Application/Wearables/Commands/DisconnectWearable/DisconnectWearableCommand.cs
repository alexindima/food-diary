using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Wearables.Commands.DisconnectWearable;

public record DisconnectWearableCommand(
    Guid? UserId,
    string Provider) : ICommand<Result>, IUserRequest;
