using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;

public sealed record BootstrapInitialAdminCommand(
    string Email,
    string Password)
    : ICommand<Result<BootstrapInitialAdminModel>>;
