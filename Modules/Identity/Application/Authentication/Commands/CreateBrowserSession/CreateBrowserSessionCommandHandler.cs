using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CreateBrowserSession;

public sealed class CreateBrowserSessionCommandHandler(BrowserSessionService sessions)
    : ICommandHandler<CreateBrowserSessionCommand, Result<BrowserSessionModel>> {
    public Task<Result<BrowserSessionModel>> Handle(CreateBrowserSessionCommand command, CancellationToken cancellationToken) =>
        sessions.CreateAsync(command.RefreshToken, command.ExistingCredential, command.MaximumExpiresAtUtc, cancellationToken);
}
