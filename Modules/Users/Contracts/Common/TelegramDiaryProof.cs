using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public sealed record TelegramDiaryProof(UserId SourceId, long SourceSecurityVersion, UserId TargetId,
    long TargetSecurityVersion, string VerifiedEmail);
