using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record TelegramDiaryTransferHttpResponse(
    [property: Required] TelegramDiaryCountsHttpResponse Counts,
    [property: Required] TelegramDiaryGoalsHttpResponse EmailGoals,
    [property: Required] TelegramDiaryGoalsHttpResponse TelegramGoals,
    bool CanUseTelegramGoals);
