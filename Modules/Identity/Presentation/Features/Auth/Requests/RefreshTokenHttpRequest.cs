using FoodDiary.Application.Abstractions.Authentication.Common;
using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RefreshTokenHttpRequest(
    [MaxLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)] string? RefreshToken
);
