using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;

public sealed record ChangeClientTaskStatusCommand(
    Guid? UserId,
    Guid TaskId,
    string Status) : ICommand<Result<ClientTaskModel>>, IUserRequest;
