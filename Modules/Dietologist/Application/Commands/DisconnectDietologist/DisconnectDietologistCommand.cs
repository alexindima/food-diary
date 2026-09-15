using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.DisconnectDietologist;

public record DisconnectDietologistCommand(
    Guid? UserId,
    Guid ClientUserId) : ICommand<Result>, IUserRequest;
