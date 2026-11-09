using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public sealed record TelegramDiaryTarget(UserId UserId, long SecurityVersion);
