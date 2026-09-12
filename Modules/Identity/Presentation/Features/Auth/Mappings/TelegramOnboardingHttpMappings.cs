using FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;
using FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;
using FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class TelegramOnboardingHttpMappings {
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
