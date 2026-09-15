using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.BeginTelegramMiniApp;

public sealed record BeginTelegramMiniAppCommand(string InitData, string BrowserBinding, Guid? LinkUserId = null)
    : ICommand<Result<TelegramAuthenticationIntentModel>>;
