using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RegisterHttpRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MinLength(6), MaxLength(256)] string Password,
    [MaxLength(16)] string? Language,
    [MaxLength(2048)] string? ClientOrigin = null
);
