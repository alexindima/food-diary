using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Security;

public sealed class RequireTelegramBotSecretAttribute() : TypeFilterAttribute(typeof(TelegramBotSecretAuthorizationFilter));
