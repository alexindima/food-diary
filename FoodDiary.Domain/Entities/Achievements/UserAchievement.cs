using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Achievements;

public sealed class UserAchievement : Entity<UserAchievementId> {
    public const int AchievementKeyMaxLength = 100;

    public UserId UserId { get; private set; }
    public string AchievementKey { get; private set; } = string.Empty;
    public DateTime EarnedAtUtc { get; private set; }
    public int EarnedValue { get; private set; }
    public int DefinitionVersion { get; private set; }

    public User User { get; private set; } = null!;

    private UserAchievement() {
    }

    public static UserAchievement Create(
        UserId userId,
        string achievementKey,
        DateTime earnedAtUtc,
        int earnedValue,
        int definitionVersion) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(achievementKey);

        string normalizedKey = achievementKey.Trim();
        if (normalizedKey.Length is 0 or > AchievementKeyMaxLength) {
            throw new ArgumentOutOfRangeException(nameof(achievementKey), $"Achievement key must contain between 1 and {AchievementKeyMaxLength} characters.");
        }

        if (earnedValue < 0) {
            throw new ArgumentOutOfRangeException(nameof(earnedValue), "Earned value cannot be negative.");
        }

        if (definitionVersion <= 0) {
            throw new ArgumentOutOfRangeException(nameof(definitionVersion), "Definition version must be positive.");
        }

        DateTime normalizedEarnedAtUtc = DomainGuard.RequiredUtc(earnedAtUtc, nameof(earnedAtUtc));

        var achievement = new UserAchievement {
            Id = UserAchievementId.New(),
            UserId = userId,
            AchievementKey = normalizedKey,
            EarnedAtUtc = normalizedEarnedAtUtc,
            EarnedValue = earnedValue,
            DefinitionVersion = definitionVersion,
        };
        achievement.SetCreated(normalizedEarnedAtUtc);
        return achievement;
    }
}
