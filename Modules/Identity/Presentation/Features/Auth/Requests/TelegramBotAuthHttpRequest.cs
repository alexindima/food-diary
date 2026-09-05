using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramBotAuthHttpRequest([Range(1, long.MaxValue)] long TelegramUserId);
