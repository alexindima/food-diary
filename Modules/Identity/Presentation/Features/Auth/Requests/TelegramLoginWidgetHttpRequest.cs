using FoodDiary.Modules.Identity.Contracts.Authentication;
using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record TelegramLoginWidgetHttpRequest(
    [Range(1, long.MaxValue)] long Id,
    [Range(1, long.MaxValue)] long AuthDate,
    [Required, MaxLength(IdentityInputLimits.MaximumTelegramHashLength)] string Hash,
    [MaxLength(IdentityInputLimits.MaximumTelegramUsernameLength)] string? Username,
    [MaxLength(IdentityInputLimits.MaximumTelegramNameLength)] string? FirstName,
    [MaxLength(IdentityInputLimits.MaximumTelegramNameLength)] string? LastName,
    [MaxLength(IdentityInputLimits.MaximumTelegramPhotoUrlLength)] string? PhotoUrl);
