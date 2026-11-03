using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramUnlink;

public sealed record CompleteTelegramUnlinkCommand(Guid? UserId, string Code, string State, string BrowserBinding)
    : ICommand<Result>, IUserRequest;
