using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.CreateUserByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record CreateUserByAdministratorCommand(
    UserAdminCreateModel Request) : IRequest<Result<UserAdminReadModel>>;
