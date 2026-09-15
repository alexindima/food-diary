using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CancelClientTask;

public sealed record CancelClientTaskCommand(
    Guid? UserId,
    Guid TaskId) : ICommand<Result<ClientTaskModel>>, IUserRequest;
