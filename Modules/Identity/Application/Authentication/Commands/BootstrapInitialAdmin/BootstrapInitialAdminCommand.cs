using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;

public sealed record BootstrapInitialAdminCommand(
    string Email,
    string Password)
    : ICommand<Result<BootstrapInitialAdminModel>>;
