using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record StartTelegramBackupEmailHttpRequest([Required, EmailAddress, MaxLength(254)] string Email);
