using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record ResendEmailVerificationHttpRequest(
    [MaxLength(2048)] string? ClientOrigin = null
);
