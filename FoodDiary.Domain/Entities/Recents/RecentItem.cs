using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Recents;

public sealed class RecentItem : Entity<RecentItemId>
{
    public UserId UserId { get; private set; }
    public RecentItemType ItemType { get; private set; }
    public Guid ItemId { get; private set; }
    public DateTime LastUsedAtUtc { get; private set; }
    public int UsageCount { get; private set; }

    public User User { get; private set; } = null!;

    private RecentItem() { }

    public static RecentItem Create(UserId userId, RecentItemType itemType, Guid itemId, DateTime? usedAtUtc = null)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("ItemId cannot be empty.", nameof(itemId));
        }

        var now = NormalizeUtc(usedAtUtc ?? DateTime.UtcNow);

        var recentItem = new RecentItem
        {
            Id = RecentItemId.New(),
            UserId = userId,
            ItemType = itemType,
            ItemId = itemId,
            LastUsedAtUtc = now,
            UsageCount = 1
        };

        recentItem.SetCreated();
        return recentItem;
    }

    public void Touch(DateTime? usedAtUtc = null)
    {
        LastUsedAtUtc = NormalizeUtc(usedAtUtc ?? DateTime.UtcNow);
        UsageCount += 1;
        SetModified();
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };
    }
}

