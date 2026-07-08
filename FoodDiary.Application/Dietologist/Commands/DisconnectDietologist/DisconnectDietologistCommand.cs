using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.DisconnectDietologist;

public record DisconnectDietologistCommand(
    Guid? UserId,
    Guid ClientUserId) : ICommand<Result>, IUserRequest;
