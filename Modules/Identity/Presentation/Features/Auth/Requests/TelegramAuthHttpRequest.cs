using FoodDiary.Modules.Identity.Contracts.Authentication;
using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record TelegramAuthHttpRequest(
    [Required, MaxLength(IdentityInputLimits.MaximumTelegramInitDataLength)] string InitData);
