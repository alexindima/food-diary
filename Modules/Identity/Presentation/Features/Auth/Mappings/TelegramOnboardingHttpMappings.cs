using FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;
using FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;
using FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Commands.StartTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Commands.StartTelegramBackupEmail;
using FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramBackupEmail;
using FoodDiary.Application.Identity.Authentication.Commands.RequestTelegramBackupEmail;
using FoodDiary.Application.Identity.Authentication.Commands.UnlinkTelegram;
using FoodDiary.Application.Identity.Authentication.Queries.GetTelegramConfiguration;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class TelegramOnboardingHttpMappings {
    public static GetTelegramConfigurationQuery ToConfigurationQuery() => new();

    public static StartTelegramOidcCommand ToStartOidcCommand(this string binding, Guid? userId = null) => new(binding, userId);

    public static RequestTelegramBackupEmailCommand ToBackupEmailCommand(this TelegramBackupEmailHttpRequest request, Guid userId) =>
        new(userId, request.Email, request.InitData);

    public static UnlinkTelegramCommand ToUnlinkCommand(this TelegramAuthHttpRequest request, Guid userId) => new(userId, request.InitData);

    public static StartTelegramBackupEmailCommand ToBackupEmailStartCommand(this StartTelegramBackupEmailHttpRequest request, Guid userId, string binding) =>
        new(userId, request.Email, binding);

    public static CompleteTelegramBackupEmailCommand ToBackupEmailCompleteCommand(this ExchangeTelegramOidcHttpRequest request, Guid userId, string binding) =>
        new(userId, request.Code, request.State, binding);

    public static ExchangeTelegramOidcCommand ToExchangeCommand(this ExchangeTelegramOidcHttpRequest request, string binding) =>
        new(request.Code, request.State, binding);

    public static TelegramOidcStartHttpResponse ToHttpResponse(this TelegramOidcStartModel model) => new(model.AuthorizationUrl);

    public static BeginTelegramMiniAppCommand ToBeginCommand(this TelegramAuthHttpRequest request, string binding, Guid? userId = null) =>
        new(request.InitData, binding, userId);

    public static CompleteTelegramAuthenticationCommand ToCompleteCommand(
        this CompleteTelegramAuthenticationHttpRequest request, string binding, HttpContext context) =>
        new(request.Ticket, binding, request.Action is "login" or "register" ? request.Action : string.Empty,
            Language: request.Language, TimeZoneId: request.TimeZoneId, ClientContext: context.ToAuthenticationClientContext("telegram"));

    public static CompleteTelegramAuthenticationCommand ToCompleteLinkCommand(
        this CompleteTelegramLinkHttpRequest request, string binding, Guid userId, HttpContext context) =>
        new(request.Ticket, binding, "link", userId, ClientContext: context.ToAuthenticationClientContext("telegram"));

    public static TelegramAuthenticationIntentHttpResponse ToHttpResponse(this TelegramAuthenticationIntentModel model) =>
        new(model.Ticket, model.NextAction, model.ExpiresAtUtc);
}
