using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record RequestPasswordResetHttpRequest(
    [Required, MaxLength(320)] string Email,
    [MaxLength(2048)] string? ClientOrigin = null
);
