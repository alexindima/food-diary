using System.ComponentModel.DataAnnotations;
using FoodDiary.Modules.Identity.Contracts.Authentication;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record DisconnectTelegramHttpRequest(
    [MaxLength(256)] string? Password = null,
    [MaxLength(IdentityInputLimits.MaximumGoogleCredentialLength)] string? GoogleCredential = null,
    bool RememberMe = false) {
    public override string ToString() => nameof(DisconnectTelegramHttpRequest);
}
