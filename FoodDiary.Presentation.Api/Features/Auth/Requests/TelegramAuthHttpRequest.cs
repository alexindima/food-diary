using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramAuthHttpRequest([Required, MaxLength(8192)] string InitData);
