using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record RefreshTokenHttpRequest(
    [Required, MaxLength(4096)] string RefreshToken
);
