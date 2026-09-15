using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.ChangeClientTaskStatus;

public sealed record ChangeClientTaskStatusCommand(
    Guid? UserId,
    Guid TaskId,
    string Status) : ICommand<Result<ClientTaskModel>>, IUserRequest;
