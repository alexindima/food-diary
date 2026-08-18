using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record AdminSsoExchangeHttpRequest(
    [Required, MaxLength(512)] string Code
);
