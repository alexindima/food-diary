using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.LogoutBrowserSession;

public sealed class LogoutBrowserSessionCommandHandler(BrowserSessionService sessions) : ICommandHandler<LogoutBrowserSessionCommand, Result> {
    public Task<Result> Handle(LogoutBrowserSessionCommand command, CancellationToken cancellationToken) =>
        sessions.LogoutAsync(command.Credential, cancellationToken);
}
