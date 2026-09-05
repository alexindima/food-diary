using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record VerifyEmailHttpRequest(
    Guid UserId,
    [Required, MaxLength(4096)] string Token
);
