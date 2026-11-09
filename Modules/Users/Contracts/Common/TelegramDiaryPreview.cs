namespace FoodDiary.Modules.Users.Contracts.Common;

public sealed record TelegramDiaryPreview(TelegramDiaryCounts Counts, TelegramDiaryGoals EmailGoals,
    TelegramDiaryGoals TelegramGoals, bool CanUseTelegramGoals);
