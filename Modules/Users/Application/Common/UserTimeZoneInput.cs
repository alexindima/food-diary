namespace FoodDiary.Modules.Users.Application.Common;

internal static class UserTimeZoneInput {
    internal static bool IsValid(string? value) => IsValid(value, TimeZoneInfo.FindSystemTimeZoneById);

    internal static bool IsValid(string? value, Func<string, TimeZoneInfo> resolveTimeZone) {
        if (value is null) {
            return true;
        }
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100) {
            return false;
        }
        try {
            TimeZoneInfo zone = resolveTimeZone(value.Trim());
            return zone.HasIanaId || string.Equals(zone.Id, "UTC", StringComparison.Ordinal);
        } catch (TimeZoneNotFoundException) {
            return false;
        } catch (InvalidTimeZoneException) {
            return false;
        }
    }
}
