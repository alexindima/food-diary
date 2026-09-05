using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record ExchangeImpersonationHttpRequest([Required, MaxLength(128)] string Code);
