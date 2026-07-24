using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CancelClientTask;

public sealed record CancelClientTaskCommand(
    Guid? UserId,
    Guid TaskId) : ICommand<Result<ClientTaskModel>>, IUserRequest;
