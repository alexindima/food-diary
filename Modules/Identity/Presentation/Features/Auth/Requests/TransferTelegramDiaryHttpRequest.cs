using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

public sealed record TransferTelegramDiaryHttpRequest(
    [Required, StringLength(43, MinimumLength = 43)] string Ticket,
    bool? UseTelegramGoals);
