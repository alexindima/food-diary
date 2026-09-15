using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record ConfirmPasswordResetHttpRequest(
    Guid UserId,
    [Required, MaxLength(4096)] string Token,
    [Required, MinLength(6), MaxLength(256)] string NewPassword
);
