using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Wearables.Application.Commands.DisconnectWearable;

public record DisconnectWearableCommand(
    Guid? UserId,
    string Provider) : ICommand<Result>, IUserRequest;
