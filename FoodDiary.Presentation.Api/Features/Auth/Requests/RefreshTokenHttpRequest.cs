using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RefreshTokenHttpRequest(
    [Required, MaxLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)] string RefreshToken
);
