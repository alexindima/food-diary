using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record TelegramBotAuthHttpRequest([Range(1, long.MaxValue)] long TelegramUserId);
