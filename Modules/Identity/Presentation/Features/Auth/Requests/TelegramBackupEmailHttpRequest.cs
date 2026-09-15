using FoodDiary.Modules.Identity.Contracts.Authentication;
using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record TelegramBackupEmailHttpRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MaxLength(IdentityInputLimits.MaximumTelegramInitDataLength)] string InitData);
