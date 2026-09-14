using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Commands.SetUserPasswordByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record SetUserPasswordByAdministratorCommand(
    FoodDiary.Domain.ValueObjects.Ids.UserId UserId,
    FoodDiary.Domain.ValueObjects.Ids.UserId ActorUserId,
    string NewPassword) : IRequest<Result>;
