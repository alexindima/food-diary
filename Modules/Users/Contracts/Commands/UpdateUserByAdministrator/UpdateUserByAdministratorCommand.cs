using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.UpdateUserByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpdateUserByAdministratorCommand(
    UserAdminUpdateModel Request) : IRequest<Result<UserAdminReadModel>>;
