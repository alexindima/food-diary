using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;

public sealed class BootstrapInitialAdminCommandHandler(
    IInitialAdminBootstrapService initialAdminBootstrapService)
    : ICommandHandler<BootstrapInitialAdminCommand, Result<BootstrapInitialAdminModel>> {
    public Task<Result<BootstrapInitialAdminModel>> Handle(
        BootstrapInitialAdminCommand command,
        CancellationToken cancellationToken) =>
        initialAdminBootstrapService.BootstrapAsync(command.Email, command.Password, cancellationToken);
}
