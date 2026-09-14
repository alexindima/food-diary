using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Commands.CreateUserByAdministrator;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record CreateUserByAdministratorCommand(
    UserAdminCreateModel Request) : IRequest<Result<UserAdminReadModel>>;
