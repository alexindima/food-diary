using System.ComponentModel.DataAnnotations;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramAuthHttpRequest(
    [Required, MaxLength(AuthenticationInputLimits.MaximumTelegramInitDataLength)] string InitData);
