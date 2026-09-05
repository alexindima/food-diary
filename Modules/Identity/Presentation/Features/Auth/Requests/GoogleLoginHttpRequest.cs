using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record GoogleLoginHttpRequest(
    [Required, MaxLength(AuthenticationInputLimits.MaximumGoogleCredentialLength)] string Credential,
    bool RememberMe = false
);
