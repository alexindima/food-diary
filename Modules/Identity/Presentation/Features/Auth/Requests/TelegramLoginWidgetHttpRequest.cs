using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramLoginWidgetHttpRequest(
    [Range(1, long.MaxValue)] long Id,
    [Range(1, long.MaxValue)] long AuthDate,
    [Required, MaxLength(AuthenticationInputLimits.MaximumTelegramHashLength)] string Hash,
    [MaxLength(AuthenticationInputLimits.MaximumTelegramUsernameLength)] string? Username,
    [MaxLength(AuthenticationInputLimits.MaximumTelegramNameLength)] string? FirstName,
    [MaxLength(AuthenticationInputLimits.MaximumTelegramNameLength)] string? LastName,
    [MaxLength(AuthenticationInputLimits.MaximumTelegramPhotoUrlLength)] string? PhotoUrl);
