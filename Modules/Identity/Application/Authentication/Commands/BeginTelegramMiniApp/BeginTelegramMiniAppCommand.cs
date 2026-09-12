using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;

public sealed record BeginTelegramMiniAppCommand(string InitData, string BrowserBinding, Guid? LinkUserId = null)
    : ICommand<Result<TelegramAuthenticationIntentModel>>;
