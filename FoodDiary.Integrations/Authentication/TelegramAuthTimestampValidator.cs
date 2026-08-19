namespace FoodDiary.Integrations.Authentication;

internal static class TelegramAuthTimestampValidator {
    internal enum Status {
        Valid = 0,
        Invalid = 1,
        Expired = 2,
    }

    private static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(5);

    public static Status Validate(
        long authDateSeconds,
        int authTtlSeconds,
        DateTime nowUtc,
        out DateTime authDateUtc) {
        authDateUtc = default;
        DateTime expiresAtUtc;
        try {
            authDateUtc = DateTimeOffset.FromUnixTimeSeconds(authDateSeconds).UtcDateTime;
            expiresAtUtc = authDateUtc.AddSeconds(authTtlSeconds);
        } catch (ArgumentOutOfRangeException) {
            return Status.Invalid;
        }

        if (authDateUtc > nowUtc.Add(MaximumFutureClockSkew)) {
            return Status.Invalid;
        }

        return nowUtc > expiresAtUtc
            ? Status.Expired
            : Status.Valid;
    }
}
