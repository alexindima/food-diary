using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Отдельный приём воды пользователя
/// </summary>
public sealed class HydrationEntry : AggregateRoot<HydrationEntryId>
{
    public UserId UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int AmountMl { get; private set; }

    public User User { get; private set; } = null!;

    private HydrationEntry()
    {
    }

    private HydrationEntry(HydrationEntryId id) : base(id)
    {
    }

    public static HydrationEntry Create(UserId userId, DateTime timestampUtc, int amountMl)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amountMl, 0);

        var entry = new HydrationEntry(HydrationEntryId.New())
        {
            UserId = userId,
            Timestamp = Normalize(timestampUtc),
            AmountMl = amountMl
        };

        entry.SetCreated();
        return entry;
    }

    public void Update(int? amountMl = null, DateTime? timestampUtc = null)
    {
        if (amountMl.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(amountMl.Value, 0);
            AmountMl = amountMl.Value;
        }

        if (timestampUtc.HasValue)
        {
            Timestamp = Normalize(timestampUtc.Value);
        }

        SetModified();
    }

    private static DateTime Normalize(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };
        return utc;
    }
}
