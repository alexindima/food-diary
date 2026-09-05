using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record AdminSsoExchangeHttpRequest(
    [Required, MaxLength(AuthenticationInputLimits.MaximumAdminSsoCodeLength)] string Code
);
