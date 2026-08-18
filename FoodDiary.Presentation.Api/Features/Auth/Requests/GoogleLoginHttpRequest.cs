using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record GoogleLoginHttpRequest(
    [Required, MaxLength(16384)] string Credential,
    bool RememberMe = false
);
