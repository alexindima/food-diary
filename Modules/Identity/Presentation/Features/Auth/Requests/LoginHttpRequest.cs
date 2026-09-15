using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record LoginHttpRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MaxLength(256)] string Password,
    bool RememberMe = false
);
