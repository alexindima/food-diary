using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record VerifyEmailHttpRequest(
    Guid UserId,
    [Required, MaxLength(4096)] string Token
);
