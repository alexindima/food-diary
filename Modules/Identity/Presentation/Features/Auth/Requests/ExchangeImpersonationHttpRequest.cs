using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record ExchangeImpersonationHttpRequest([Required, MaxLength(128)] string Code);
