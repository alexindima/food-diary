using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;

public sealed record CompleteTelegramAuthenticationCommand(
    string Ticket,
    string BrowserBinding,
    string Action,
    Guid? CurrentUserId = null,
    string? Language = null,
    string? TimeZoneId = null,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
