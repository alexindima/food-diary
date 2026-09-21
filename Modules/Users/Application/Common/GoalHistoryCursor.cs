using System.Globalization;
using System.Text;

namespace FoodDiary.Modules.Users.Application.Common;

internal static class GoalHistoryCursor {
    public const int PageSize = 10;
    // Closed goals cannot be edited. Freeze the upper end time so later closures
    // do not shift offsets in an already-open history traversal.
    public static string Encode(DateTime snapshotUtc, int offset) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{snapshotUtc.Ticks.ToString(CultureInfo.InvariantCulture)}:{offset.ToString(CultureInfo.InvariantCulture)}"));

    public static bool TryDecode(string? cursor, DateTime nowUtc, out DateTime snapshotUtc, out int offset) {
        snapshotUtc = nowUtc;
        offset = 0;
        if (cursor is null) { return true; }
        if (cursor.Length > 128) { return false; }
        try {
            string[] parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':');
            if (parts.Length != 2 || !long.TryParse(parts[0], CultureInfo.InvariantCulture, out long ticks)
                || ticks < DateTime.MinValue.Ticks || ticks > nowUtc.Ticks
                || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out offset) || offset < 0 || offset > int.MaxValue - PageSize) {
                return false;
            }
            snapshotUtc = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        } catch (FormatException) {
            return false;
        }
    }
}
