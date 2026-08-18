using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramLoginWidgetHttpRequest(
    [Range(1, long.MaxValue)] long Id,
    [Range(1, long.MaxValue)] long AuthDate,
    [Required, MaxLength(256)] string Hash,
    [MaxLength(64)] string? Username,
    [MaxLength(128)] string? FirstName,
    [MaxLength(128)] string? LastName,
    [MaxLength(2048)] string? PhotoUrl);
