using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record ExchangeTelegramOidcHttpRequest([Required, MaxLength(4096)] string Code, [Required, StringLength(43, MinimumLength = 43)] string State);
