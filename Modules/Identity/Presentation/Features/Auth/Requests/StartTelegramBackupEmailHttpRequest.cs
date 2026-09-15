using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record StartTelegramBackupEmailHttpRequest([Required, EmailAddress, MaxLength(254)] string Email);
