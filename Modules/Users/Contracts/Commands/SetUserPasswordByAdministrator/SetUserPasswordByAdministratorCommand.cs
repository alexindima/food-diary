using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.SetUserPasswordByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record SetUserPasswordByAdministratorCommand(
    FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId UserId,
    FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids.UserId ActorUserId,
    string NewPassword) : IRequest<Result>;
