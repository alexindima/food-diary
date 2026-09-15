using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin;

public sealed record BootstrapInitialAdminCommand(
    string Email,
    string Password)
    : IRequest<Result<BootstrapInitialAdminModel>>;
