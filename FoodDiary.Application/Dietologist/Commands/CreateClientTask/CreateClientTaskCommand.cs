using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateClientTask;

public sealed record CreateClientTaskCommand(
    Guid? UserId,
    Guid ClientUserId,
    string Title,
    string? Details,
    DateTime? DueAtUtc) : ICommand<Result<ClientTaskModel>>, IUserRequest;
