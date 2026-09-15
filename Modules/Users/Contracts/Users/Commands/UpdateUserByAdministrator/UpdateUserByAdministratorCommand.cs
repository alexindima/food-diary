using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Commands.UpdateUserByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpdateUserByAdministratorCommand(
    UserAdminUpdateModel Request) : IRequest<Result<UserAdminReadModel>>;
