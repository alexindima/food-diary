using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CreateClientTask;

public sealed record CreateClientTaskCommand(
    Guid? UserId,
    Guid ClientUserId,
    string Title,
    string? Details,
    DateTime? DueAtUtc) : ICommand<Result<ClientTaskModel>>, IUserRequest;
