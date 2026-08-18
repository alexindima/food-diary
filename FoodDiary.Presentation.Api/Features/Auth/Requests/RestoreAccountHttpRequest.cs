using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RestoreAccountHttpRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MinLength(6), MaxLength(256)] string Password,
    bool RememberMe = false
);
