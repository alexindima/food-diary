using FoodDiary.Modules.Identity.Contracts.Authentication;
using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record GoogleLoginHttpRequest(
    [Required, MaxLength(IdentityInputLimits.MaximumGoogleCredentialLength)] string Credential,
    bool RememberMe = false
);
