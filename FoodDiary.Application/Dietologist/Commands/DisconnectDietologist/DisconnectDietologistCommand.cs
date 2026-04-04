using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;

public record DisconnectDietologistCommand(
    Guid? UserId,
    Guid ClientUserId) : ICommand<Result>, IUserRequest;
