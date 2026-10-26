using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RestoreBrowserSession;

public sealed class RestoreBrowserSessionCommandHandler(BrowserSessionService sessions)
    : ICommandHandler<RestoreBrowserSessionCommand, Result<BrowserSessionModel>> {
    public Task<Result<BrowserSessionModel>> Handle(RestoreBrowserSessionCommand command, CancellationToken cancellationToken) =>
        sessions.RestoreAsync(command.Credential, cancellationToken);
}
