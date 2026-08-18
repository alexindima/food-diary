using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record ResendEmailVerificationHttpRequest(
    [MaxLength(2048)] string? ClientOrigin = null
);
