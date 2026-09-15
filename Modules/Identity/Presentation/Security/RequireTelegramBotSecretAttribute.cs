using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class RequireTelegramBotSecretAttribute() : TypeFilterAttribute(typeof(TelegramBotSecretAuthorizationFilter));
